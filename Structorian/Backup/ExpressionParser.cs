using System;
using System.Collections.Generic;

namespace Structorian.Engine
{
    public class ExpressionParser
    {
        private static readonly ExprTokenType[] _condComboTokens = new[]
            {
                ExprTokenType.AND, ExprTokenType.OR
            };
        private static readonly ExprTokenType[] _condTokens = new[]
            {
                ExprTokenType.GT, ExprTokenType.GE, ExprTokenType.EQ, ExprTokenType.NE,
                ExprTokenType.LT, ExprTokenType.LE
            };
        private static readonly ExprTokenType[] _exprTokens = new[]
            {
                ExprTokenType.Plus, ExprTokenType.Minus, ExprTokenType.BitAND, ExprTokenType.BitOR,
                ExprTokenType.SHL, ExprTokenType.SHR
            };
        private static readonly ExprTokenType[] _termTokens = new[]
            {
                ExprTokenType.Mult, ExprTokenType.Div, ExprTokenType.Mod
            };
        
        private delegate Expression StepDownDelegate(ExpressionLexer lexer);
        private delegate Expression CreateBinaryDelegate(ExprTokenType operation, Expression lhs, Expression rhs);
        
        public static Expression Parse(string source)
        {
            var lexer = new ExpressionLexer(source);

            Expression result = ParseTernary(lexer);
            lexer.GetNextToken(ExprTokenType.EOF);
            result.Source = source;
            return result;
        }

        private static Expression RecursiveDescentStep(ExpressionLexer lexer, ExprTokenType[] tokens,
            StepDownDelegate stepDown, CreateBinaryDelegate createBinary)
        {
            Expression expr = stepDown(lexer);
            while (true)
            {
                ExprTokenType? token = lexer.CheckNextToken(tokens);
                if (token.HasValue)
                    expr = createBinary(token.Value, expr, stepDown(lexer));
                else
                    break;
            }
            return expr;
        }

        private static Expression ParseTernary(ExpressionLexer lexer)
        {
            var expr = ParseCondCombo(lexer);
            if (lexer.PeekNextToken() == ExprTokenType.QuestionMark)
            {
                lexer.GetNextToken(ExprTokenType.QuestionMark);
                var trueValue = ParseTernary(lexer);
                lexer.GetNextToken(ExprTokenType.Colon);
                var falseValue = ParseTernary(lexer);
                return new TernaryExpression(expr, trueValue, falseValue);
            }
            return expr;
        }


        private static Expression ParseCondCombo(ExpressionLexer lexer)
        {
            return RecursiveDescentStep(lexer, _condComboTokens,
                ParseCond,
                (o, lhs, rhs) => new LogicalExpression(o, lhs, rhs));
        }

        private static Expression ParseCond(ExpressionLexer lexer)
        {
            return RecursiveDescentStep(lexer, _condTokens,
                ParseExpr,
                (o, lhs, rhs) => new CompareExpression(o, lhs, rhs));
        }

        private static Expression ParseExpr(ExpressionLexer lexer)
        {
            return RecursiveDescentStep(lexer, _exprTokens,
                ParseTerm,
                (o, lhs, rhs) => new BinaryExpression(o, lhs, rhs));
        }
        
        private static Expression ParseTerm(ExpressionLexer lexer)
        {
            return RecursiveDescentStep(lexer, _termTokens,
                ParseFactor,
                (o, lhs, rhs) => new BinaryExpression(o, lhs, rhs));
        }
        
        private static Expression ParseFactor(ExpressionLexer lexer)
        {
            ExprTokenType tokenType = lexer.PeekNextToken();
            if (tokenType == ExprTokenType.Number)
                return new PrimitiveExpression((int)lexer.GetNextToken(ExprTokenType.Number));
            if (tokenType == ExprTokenType.String)
                return new PrimitiveExpression((string)lexer.GetNextToken(ExprTokenType.String));
            if (tokenType == ExprTokenType.Symbol)
            {
                string symbol = (string) lexer.GetNextToken(ExprTokenType.Symbol);
                if (lexer.PeekNextToken() == ExprTokenType.Dot)
                {
                    lexer.GetNextToken(ExprTokenType.Dot);
                    Expression exprInContext = ParseFactor(lexer);
                    return new ContextExpression(symbol, exprInContext, new Expression[0]);
                }
                
                if (lexer.PeekNextToken() == ExprTokenType.Open)
                {
                    lexer.GetNextToken(ExprTokenType.Open);
                    var parameters = new List<Expression>();
                    while (lexer.PeekNextToken() != ExprTokenType.Close)
                    {
                        if (parameters.Count > 0)
                            lexer.GetNextToken(ExprTokenType.Comma);
                        parameters.Add(ParseCondCombo(lexer));
                    }
                    lexer.GetNextToken(ExprTokenType.Close);
                    if (lexer.PeekNextToken() == ExprTokenType.Dot)
                    {
                        lexer.GetNextToken(ExprTokenType.Dot);
                        Expression exprInContext = ParseFactor(lexer);
                        return new ContextExpression(symbol, exprInContext, parameters.ToArray());
                    }
                    return new FunctionExpression(symbol, parameters.ToArray());
                }
                return new SymbolExpression(symbol);
            }
            if (tokenType == ExprTokenType.Open)
            {
                lexer.GetNextToken(ExprTokenType.Open);
                Expression result = ParseCondCombo(lexer);
                lexer.GetNextToken(ExprTokenType.Close);
                return result;
            }
            if (tokenType == ExprTokenType.Minus || tokenType == ExprTokenType.NOT)
            {
                lexer.GetNextToken(tokenType);
                return new UnaryExpression(ParseFactor(lexer), tokenType);
            }
            throw new ParseException("Unexpected token " + tokenType, lexer.CurrentPosition);
        }
    }
}
