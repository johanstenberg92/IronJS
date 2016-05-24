﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace IronJS.Interpreter
{
    public class Parser
    {
        private Token[] _tokens;

        private Position[] _positions;

        private int _position = 0;

        public Parser(Scanner scanner)
        {
            Lexer lexer = new Lexer(scanner);
            Tuple<Token, Position>[] tokensAndPositions = lexer.Tokenize();

            var tokens = new List<Token>();
            var positions = new List<Position>();

            foreach (Tuple<Token, Position> tuple in tokensAndPositions)
            {
                var token = tuple.Item1;
                var position = tuple.Item2;

                tokens.Add(token);
                positions.Add(position);
            }

            _tokens = tokens.ToArray();
            _positions = positions.ToArray();
        }

        public ProgramNode Parse()
        {
            return program();
        }

        private ProgramNode program()
        {
            var statements = Statements();

            return new ProgramNode(statements);
        }

        private StatementNode[] Statements()
        {
            var statements = new List<StatementNode>();

            var statement = Statement();

            while (statement != null)
            {
                statements.Add(statement);
                statement = Statement();
            }

            return statements.ToArray();
        }

        private StatementNode Statement()
        {
            var position = Position();

            if (Found(KeywordToken.Var))
            {
                var identifier = ExpectIdentifier();
                Expect(SymbolToken.Assign);

                var expression = Expression();

                Expect(SymbolToken.SemiColon);

                return new VarStatementNode(identifier, expression, position);
            }
            
            Token[] conditionals = { KeywordToken.If, KeywordToken.While };
            var conditionalsIndex = FoundOneOf(conditionals);

            if (conditionalsIndex != -1)
            {
                Expect(SymbolToken.Parenthesis);

                var expression = Expression();

                Expect(SymbolToken.ClosingParenthesis);

                StatementNode[] statements = null;

                if (Found(SymbolToken.Bracket))
                {
                    statements = Statements();
                    Expect(SymbolToken.ClosingBracket);
                }
                else
                {
                    statements = new StatementNode[] { Statement() };
                }

                if (conditionalsIndex == 1)
                    return new WhileStatementNode(expression, statements, position);

                var elseIfExpressions = new List<ExpressionNode>();
                var elseIfStatements = new List<StatementNode[]>();

                var foundElse = Found(KeywordToken.Else);
                var foundIf = Found(KeywordToken.If);

                while (foundElse && foundIf)
                {
                    Expect(SymbolToken.Parenthesis);
                    var expr = Expression();
                    Expect(SymbolToken.ClosingParenthesis);

                    StatementNode[] stmts = null;

                    if (Found(SymbolToken.Bracket))
                    {
                        stmts = Statements();
                        Expect(SymbolToken.ClosingBracket);
                    }
                    else
                    {
                        stmts = new StatementNode[] { Statement() };
                    }

                    elseIfExpressions.Add(expr);
                    elseIfStatements.Add(stmts);

                    foundElse = Found(KeywordToken.Else);
                    foundIf = Found(KeywordToken.If);
                }

                StatementNode[] elseStatements = new StatementNode[0];

                if (foundElse)
                {
                    if (Found(SymbolToken.Bracket))
                    {
                        elseStatements = Statements();
                        Expect(SymbolToken.ClosingBracket);
                    }
                    else
                    {
                        elseStatements = new StatementNode[] { Statement() };
                    }
                }

                return new IfStatementNode(
                    expression, 
                    statements, 
                    elseIfExpressions.ToArray(),
                    elseIfStatements.ToArray(),
                    elseStatements,
                    position
                );
            }
            else if (Found(KeywordToken.Function))
            {
                var identifier = ExpectIdentifier();

                Expect(SymbolToken.Parenthesis);

                var parameters = new List<string>();

                var parameter = FoundIdentifier();

                if (parameter != null)
                {
                    parameters.Add(parameter);

                    while (Found(SymbolToken.Comma))
                    {
                        parameter = ExpectIdentifier();
                        parameters.Add(parameter);
                    }
                }

                Expect(SymbolToken.ClosingParenthesis);

                Expect(SymbolToken.Bracket);

                var statements = Statements();

                ExpressionNode returnExpr = null;

                if (Found(KeywordToken.Return))
                {
                    returnExpr = Expression();
                    Expect(SymbolToken.SemiColon);
                }

                Expect(SymbolToken.ClosingBracket);

                return new FunctionStatementNode(identifier, parameters.ToArray(), statements, returnExpr, position);
            }

            var maybeProperty = FoundProperty();
            
            if (maybeProperty != null)
            {
                if (Found(SymbolToken.Assign))
                {
                    var expression = Expression();

                    return new AssignmentStatementNode(maybeProperty, expression, position);
                }
                else if (Found(SymbolToken.Parenthesis))
                {
                    var tuple = FunctionCallTuple(maybeProperty, position);
                    return new FunctionCallStatementNode(tuple.Item1, tuple.Item2);
                }
                else
                {
                    Token[] withOperations = {
                        SymbolToken.IncrementWith,
                        SymbolToken.DecrementWith,
                        SymbolToken.MultiplyWith,
                        SymbolToken.DivideWith
                    };
                    
                    var idx = ExpectOneOf(withOperations);

                    var expression = Expression();

                    Expect(SymbolToken.SemiColon);

                    char op = '+';

                    if (idx == 0) op = '+';
                    else if (idx == 1) op = '-';
                    else if (idx == 2) op = '*';
                    else op = '/';

                    return new OperatorEqualStatementNode(maybeProperty, op, expression);
                }
            }

            return null;
        }

        private ExpressionNode Expression()
        {
            var position = Position();

            Token[] addOrSubtract = { SymbolToken.Add, SymbolToken.Subtract };

            var idx = FoundOneOf(addOrSubtract);
            char optionalOp = idx != -1 ? (idx == 0 ? '+' : '-') : unchecked ((char) -1);

            var term = Term();

            return new TermExpressionNode(optionalOp, term, position);
        }

        private static Dictionary<Token, string> infixOps = new Dictionary<Token, string>()
        {
            { SymbolToken.Multiply, "*" },
            { SymbolToken.Divide, "/" },
            { SymbolToken.Add, "+" },
            { SymbolToken.Subtract, "-" },
            { SymbolToken.Equal, "==" },
            { SymbolToken.LargerThanOrEqual, ">=" },
            { SymbolToken.LessThanOrEqual, "<=" },
            { SymbolToken.NotEqual, "!=" },
            { SymbolToken.LessThan, "<" },
            { SymbolToken.LargerThan, ">" },
            { SymbolToken.Or, "||" },
            { SymbolToken.And, "&&" }
        };

        private TermNode Term()
        {
            var position = Position();
            var factor = Factor();

            var infixOpsKeys = infixOps.Keys.ToArray();

            var optionalOps = new List<string>();

            var optionalFactors = new List<FactorNode>();

            var idx = FoundOneOf(infixOpsKeys);

            while (idx != -1)
            {
                var token = infixOpsKeys[idx];
                var op = infixOps[token];

                optionalOps.Add(op);
                optionalFactors.Add(Factor());
                
                idx = FoundOneOf(infixOpsKeys);
            }

            return new TermNode(factor, optionalOps.ToArray(), optionalFactors.ToArray());
        }

        private FactorNode Factor()
        {
            var position = Position();

            var maybeProperty = FoundProperty();

            if (maybeProperty != null)
            {
                if (Found(SymbolToken.Parenthesis))
                {
                    var tuple = FunctionCallTuple(maybeProperty, position);
                    return new FunctionCallFactorNode(tuple.Item1, tuple.Item2);
                }

                return new PropertyFactorNode(maybeProperty);
            }

            var maybeNumber = FoundNumber();

            if (maybeNumber.HasValue) return new NumberFactorNode(maybeNumber.Value, position);

            var maybeString = FoundString();

            if (maybeString != null) return new StringFactorNode(maybeString, position);

            Expect(SymbolToken.Parenthesis);
            var expression = Expression();
            Expect(SymbolToken.ClosingParenthesis);

            return new ExpressionFactorNode(expression, position);
        }

        private PropertyNode FoundProperty()
        {
            var position = Position();

            var identifier = FoundIdentifier();

            if (identifier != null)
            {
                var identifiers = new List<string>();

                identifiers.Add(identifier);

                while (Found(SymbolToken.Dot))
                {
                    identifier = ExpectIdentifier();
                    identifiers.Add(identifier);
                }

                return new PropertyNode(identifiers.ToArray(), position);
            }

            return null;
        }

        private PropertyNode ExpectProperty()
        {
            var property = FoundProperty();

            if (property == null)
            {
                ThrowParserError();
            }

            return property;
        }

        private bool Found(Token token)
        {
            if (token == _tokens[_position])
            {
                _position++;

                return true;
            }

            return false;
        }

        private string FoundIdentifier()
        {
            var token = _tokens[_position] as IdentifierToken;

            if (token != null)
            {
                _position++;
                return token.Value;
            }

            return null;
        }

        private int? FoundNumber()
        {
            var token = _tokens[_position] as NumericToken;

            if (token != null)
            {
                _position++;
                return token.Value;
            }

            return null;
        }

        private string FoundString()
        {
            var token = _tokens[_position] as StringToken;

            if (token != null)
            {
                _position++;
                return token.Value;
            }

            return null;
        }

        private void Expect(Token token)
        {
            if (token != _tokens[_position])
            {
                ThrowParserError();
            }

            _position++;
        }

        private string ExpectIdentifier()
        {
            var maybeIdentifier = FoundIdentifier();
            if (maybeIdentifier == null) ThrowParserError();
            return maybeIdentifier;
        }

        private void ThrowParserError()
        {
            var position = _positions[_position];
            throw new InvalidOperationException($"Parsing failed at row {position.Row}, column {position.Column}");
        }

        private Position Position()
        {
            return _positions[_position];
        }

        private int FoundOneOf(Token[] tokens)
        {
            var t = _tokens[_position];
            for (int i = 0; i < tokens.Length; ++i)
            {
                if (tokens[i] == t)
                {
                    _position++;
                    return i;
                }
            }
            
            return -1;
        }

        private int ExpectOneOf(Token[] tokens)
        {
            var res = FoundOneOf(tokens);

            if (res == -1)
            {
                ThrowParserError();
            }

            return res;
        }

        private Tuple<PropertyNode, ExpressionNode[]> FunctionCallTuple(PropertyNode property, Position position)
        {
            var parameters = new List<ExpressionNode>();

            if (!Found(SymbolToken.ClosingParenthesis))
            {
                var parameter = Expression();

                if (parameter != null)
                {
                    parameters.Add(parameter);

                    while (Found(SymbolToken.Comma))
                    {
                        parameter = Expression();
                        if (parameter == null) ThrowParserError();
                        parameters.Add(parameter);
                    }
                }

                Expect(SymbolToken.ClosingParenthesis);
            }
            Expect(SymbolToken.SemiColon);

            return new Tuple<PropertyNode, ExpressionNode[]>(property, parameters.ToArray());
        }
    }
}