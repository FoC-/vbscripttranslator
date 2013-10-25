﻿using VBScriptTranslator.LegacyParser.CodeBlocks.Basic;

namespace CSharpWriter.CodeTranslation
{
    public interface ITranslateIndividualStatements
    {
		/// <summary>
		/// This will never return null or blank, it will raise an exception if unable to satisfy the request (this includes the case of a null statement reference)
		/// </summary>
        string Translate(Statement statement, ScopeAccessInformation scopeAccessInformation);
        
		/// <summary>
		/// This will never return null or blank, it will raise an exception if unable to satisfy the request (this includes the case of a null statement reference)
		/// </summary>
        string Translate(Expression expression, ScopeAccessInformation scopeAccessInformation, ExpressionReturnTypeOptions returnRequirements);
    }
}
