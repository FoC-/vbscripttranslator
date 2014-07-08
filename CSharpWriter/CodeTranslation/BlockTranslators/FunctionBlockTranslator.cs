﻿using CSharpSupport.Attributes;
using CSharpWriter.CodeTranslation.Extensions;
using CSharpWriter.CodeTranslation.StatementTranslation;
using CSharpWriter.Lists;
using CSharpWriter.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VBScriptTranslator.LegacyParser.CodeBlocks;
using VBScriptTranslator.LegacyParser.CodeBlocks.Basic;

namespace CSharpWriter.CodeTranslation.BlockTranslators
{
    public class FunctionBlockTranslator : CodeBlockTranslator
    {
        public FunctionBlockTranslator(
            CSharpName supportRefName,
            CSharpName envClassName,
            CSharpName envRefName,
            CSharpName outerClassName,
            CSharpName outerRefName,
            VBScriptNameRewriter nameRewriter,
            TempValueNameGenerator tempNameGenerator,
			ITranslateIndividualStatements statementTranslator,
			ITranslateValueSettingsStatements valueSettingStatementTranslator,
            ILogInformation logger)
            : base(supportRefName, envClassName, envRefName, outerClassName, outerRefName, nameRewriter, tempNameGenerator, statementTranslator, valueSettingStatementTranslator, logger) { }

		public TranslationResult Translate(AbstractFunctionBlock functionBlock, ScopeAccessInformation scopeAccessInformation, int indentationDepth)
		{
			if (functionBlock == null)
				throw new ArgumentNullException("functionBlock");
			if (scopeAccessInformation == null)
				throw new ArgumentNullException("scopeAccessInformation");
            if (indentationDepth < 0)
                throw new ArgumentOutOfRangeException("indentationDepth", "must be zero or greater");

            var returnValueName = functionBlock.HasReturnValue ? _tempNameGenerator(new CSharpName("retVal"), scopeAccessInformation) : null;
			var translationResult = TranslationResult.Empty.Add(
				TranslateFunctionHeader(
					functionBlock,
					returnValueName,
					indentationDepth
				)
			);
            CSharpName errorRegistrationTokenIfAny;
            if (functionBlock.Statements.ToNonNullImmutableList().DoesScopeContainOnErrorResumeNext())
            {
                errorRegistrationTokenIfAny = _tempNameGenerator(new CSharpName("errOn"), scopeAccessInformation);
                translationResult = translationResult.Add(new TranslatedStatement(
                    string.Format(
                        "var {0} = {1}.GETERRORTRAPPINGTOKEN();",
                        errorRegistrationTokenIfAny.Name,
                        _supportRefName.Name
                    ),
                    indentationDepth + 1
                ));
            }
            else
                errorRegistrationTokenIfAny = null;
            translationResult = translationResult.Add(
				Translate(
				    functionBlock.Statements.ToNonNullImmutableList(),
					    scopeAccessInformation.Extend(
                            functionBlock,
                            returnValueName,
                            errorRegistrationTokenIfAny,
                            functionBlock.Statements.ToNonNullImmutableList()
                        ),
				    indentationDepth + 1
				)
			);
            if (errorRegistrationTokenIfAny != null)
            {
                translationResult = translationResult.Add(new TranslatedStatement(
                    string.Format(
                        "{0}.RELEASEERRORTRAPPINGTOKEN({1});",
                        _supportRefName.Name,
                        errorRegistrationTokenIfAny.Name
                    ),
                    indentationDepth + 1
                ));
            }
            if (functionBlock.HasReturnValue)
			{
				// If this is an empty function then just render "return null" (TranslateFunctionHeader won't declare the return value reference) 
				translationResult = translationResult.Add(
					new TranslatedStatement(
						string.Format(
							"return {0};",
							functionBlock.Statements.Any() ? returnValueName.Name : "null"
						),
						indentationDepth + 1
					)
				);
			}
			return translationResult.Add(
				new TranslatedStatement("}", indentationDepth)
            );
		}

		private TranslationResult Translate(NonNullImmutableList<ICodeBlock> blocks, ScopeAccessInformation scopeAccessInformation, int indentationDepth)
		{
			if (blocks == null)
				throw new ArgumentNullException("block");
			if (scopeAccessInformation == null)
				throw new ArgumentNullException("scopeAccessInformation");
			if (indentationDepth < 0)
				throw new ArgumentOutOfRangeException("indentationDepth", "must be zero or greater");

			return base.TranslateCommon(
				new BlockTranslationAttempter[]
				{
					base.TryToTranslateBlankLine,
					base.TryToTranslateClass,
					base.TryToTranslateComment,
					base.TryToTranslateDim,
					base.TryToTranslateDo,
					base.TryToTranslateExit,
					base.TryToTranslateFor,
					base.TryToTranslateForEach,
					base.TryToTranslateIf,
                    base.TryToTranslateOnErrorResumeNext,
                    base.TryToTranslateOnErrorGotoZero,
					base.TryToTranslateReDim,
					base.TryToTranslateRandomize,
					base.TryToTranslateStatementOrExpression,
					base.TryToTranslateSelect,
                    base.TryToTranslateValueSettingStatement
				}.ToNonNullImmutableList(),
				blocks,
				scopeAccessInformation,
				indentationDepth
			);
		}

		private IEnumerable<TranslatedStatement> TranslateFunctionHeader(AbstractFunctionBlock functionBlock, CSharpName returnValueNameIfAny, int indentationDepth)
		{
			if (functionBlock == null)
				throw new ArgumentNullException("functionBlock");
			if (functionBlock.HasReturnValue && (returnValueNameIfAny == null))
				throw new ArgumentException("returnValueNameIfAny must not be null if functionBlock.HasReturnValue is true");
			if (indentationDepth < 0)
				throw new ArgumentOutOfRangeException("indentationDepth", "must be zero or greater");

			var content = new StringBuilder();
			content.Append(functionBlock.IsPublic ? "public" : "private");
			content.Append(" ");
			content.Append(functionBlock.HasReturnValue ? "object" : "void");
			content.Append(" ");
			content.Append(_nameRewriter.GetMemberAccessTokenName(functionBlock.Name));
			content.Append("(");
			var numberOfParameters = functionBlock.Parameters.Count();
			for (var index = 0; index < numberOfParameters; index++)
			{
				var parameter = functionBlock.Parameters.ElementAt(index);
				if (parameter.ByRef)
					content.Append("ref ");
                content.Append("object ");
                content.Append(_nameRewriter.GetMemberAccessTokenName(parameter.Name));
                if (index < (numberOfParameters - 1))
					content.Append(", ");
			}
			content.Append(")");

			var translatedStatements = new List<TranslatedStatement>();
			if (functionBlock.IsDefault)
				translatedStatements.Add(new TranslatedStatement("[" + typeof(IsDefault).FullName + "]", indentationDepth));
            var property = functionBlock as PropertyBlock;
            if ((property != null) && property.IsPublic && property.IsIndexedProperty())
            {
                translatedStatements.Add(
                    new TranslatedStatement(
                        string.Format(
                            "[" + typeof(TranslatedProperty).FullName + "({0})]",
                            property.Name.Content.ToLiteral()
                        ),
                        indentationDepth
                    )
                );
            }
            translatedStatements.Add(new TranslatedStatement(content.ToString(), indentationDepth));
            translatedStatements.Add(new TranslatedStatement("{", indentationDepth));
			if (functionBlock.HasReturnValue && functionBlock.Statements.Any())
			{
				translatedStatements.Add(new TranslatedStatement(
					base.TranslateVariableInitialisation(
						new VariableDeclaration(
							new DoNotRenameNameToken(
                                returnValueNameIfAny.Name,
                                functionBlock.Name.LineIndex
                            ),
							VariableDeclarationScopeOptions.Private,
                            null // Not declared as an array
                        ),
                        ScopeLocationOptions.WithinFunctionOrProperty
					),
					indentationDepth + 1
				));
			}
            return translatedStatements;
		}
    }
}
