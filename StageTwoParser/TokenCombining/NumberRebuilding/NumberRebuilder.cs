﻿using System;
using System.Collections.Generic;
using System.Linq;
using VBScriptTranslator.LegacyParser.Tokens;
using VBScriptTranslator.LegacyParser.Tokens.Basic;
using VBScriptTranslator.StageTwoParser.TokenCombining.NumberRebuilding.States;
using VBScriptTranslator.StageTwoParser.Tokens;

namespace VBScriptTranslator.StageTwoParser.TokenCombining.NumberRebuilding
{
    /// <summary>
    /// Given the output of a StringBreaker/TokenBreaker pass, this will try to reconstruct numbers into single AtomTokens. Doing so will allow
    /// any remaining MemberAccessorOrDecimalPointToken to be replaced with MemberAccessorTokens (there will no longer be any ambiguity). As
    /// part of this processing, the "optional zeros" will be explicitly expressed (".1" will be replaced with "0.1"). All numeric values
    /// will be included in the output as NumericValueToken instances.
    /// </summary>
    public static class NumberRebuilder
    {
        public static IEnumerable<IToken> Rebuild(IEnumerable<IToken> tokens)
        {
            if (tokens == null)
                throw new ArgumentNullException("tokens");

            // Note: There are some limitations to what can be done here - for example "Test -1" can not be have the "-" and "1" recombined since,
            // at this point, we don't know if "Test" is a value that 1 should be subtracted from or whether it's a function that is being
            // called with "-1" as the argument. However, for some values of "Test", we CAN be sure that it's not a substraction; if the
            // value is a keyword or a comparison operator (as in the "FOR", "TO" and "=" in "FOR a = -1 TO -10 STEP -1" then the minus
            // sign must be part of a numeric value).

            // At the beginning of a token set, if the first token is a minus sign then it is elligible to be part of a number. If it is not the
            // first token then it may only incorporated into a number if preceded by another operator or an opening brace (which effectively
            // would make the token the start of a new expression). The same principle applies to a decimal point. This is why the initial
            // processor is a PeriodOrMinusSignOrNumberCouldIndicateStartOfNumber.
            var processor = (IAmLookingForNumberContent)PeriodOrMinusSignOrNumberCouldIndicateStartOfNumber.Instance;
            var numberContent = new PartialNumberContent();
            var rebuiltTokens = new List<IToken>();
            var tokenArray = tokens.ToArray();
            if (tokenArray.Any(t => t == null))
                throw new ArgumentException("Null reference encountered in tokens set");
            for (var index = 0; index < tokenArray.Length; index++)
            {
                var result = processor.Process(tokenArray.Skip(index), numberContent);
                if (result.ProcessedTokens.Any())
                    rebuiltTokens.AddRange(result.ProcessedTokens);
                processor = result.NextProcessor;
                numberContent = result.NumberContent;
            }
            if (numberContent.Tokens.Any())
            {
                var numbericValueToken = numberContent.TryToExpressNumericValueTokenFromCurrentTokens();
                if (numbericValueToken == null)
                    rebuiltTokens.AddRange(numberContent.Tokens);
                else
                    rebuiltTokens.Add(numbericValueToken);
            }
            return rebuiltTokens.Select(t => (t is MemberAccessorOrDecimalPointToken) ? new MemberAccessorToken(t.LineIndex) : t);
        }
    }
}
