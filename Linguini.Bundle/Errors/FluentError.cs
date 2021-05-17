﻿using System;
using Linguini.Shared.Util;
using Linguini.Syntax.Ast;
using Linguini.Syntax.Parser.Error;

namespace Linguini.Bundle.Errors
{
    public abstract record FluentError
    {
        public abstract ErrorType ErrorKind();
    }

    public record OverrideFluentError : FluentError
    {
        private readonly string _id;
        private readonly EntryKind _kind;
    
        public OverrideFluentError(string id, EntryKind kind)
        {
            _id = id;
            _kind = kind;
        }

        public override ErrorType ErrorKind()
        {
            return ErrorType.Overriding;
        }

        public override string ToString()
        {
            return $"For id:{_id} already exist entry of type: {_kind.ToString()}";
        }
    }

    public record ResolverFluentError : FluentError
    {
        private string Description;
        private ErrorType Kind;

        private ResolverFluentError(string desc, ErrorType kind)
        {
            Description = desc;
            Kind = kind;
        }

        public override ErrorType ErrorKind()
        {
            return Kind;
        }

        public override string ToString()
        {
            return Description;
        }

        public static ResolverFluentError NoValue(ReadOnlyMemory<char> idName)
        {
            return new($"No value: {idName.Span.ToString()}", ErrorType.NoValue);
        }
        
        public static ResolverFluentError NoValue(string pattern)
        {
            return new($"No value: {pattern}", ErrorType.NoValue);
        }

        public static ResolverFluentError UnknownVariable(VariableReference outType)
        {
            return new($"Unknown variable: ${outType.Id}", ErrorType.Reference);
        }

        public static ResolverFluentError TooManyPlaceables()
        {
            return new("Too many placeables", ErrorType.TooManyPlaceables);
        }

        public static ResolverFluentError Reference(IInlineExpression self)
        {
            // TODO only allow references here
            if (self.TryConvert(out FunctionReference? funcRef))
            {
                return new($"Unknown function: {funcRef.Id}()", ErrorType.Reference);
            }

            if (self.TryConvert(out MessageReference? msgRef))
            {
                if (msgRef.Attribute == null)
                {
                    return new($"Unknown message: {msgRef.Id}", ErrorType.Reference);
                }

                return new($"Unknown attribute: {msgRef.Id}.{msgRef.Attribute}", ErrorType.Reference);
            }

            if (self.TryConvert(out TermReference? termReference))
            {
                if (termReference.Attribute == null)
                {
                    return new($"Unknown term: -{termReference.Id}", ErrorType.Reference);
                }

                return new($"Unknown attribute: -{termReference.Id}.{termReference.Attribute}", ErrorType.Reference);
            }

            if (self.TryConvert(out VariableReference? varRef))
            {
                return new($"Unknown variable: ${varRef.Id}", ErrorType.Reference);
            }

            throw new ArgumentException($"Expected reference got ${self.GetType()}");
        }

        public static ResolverFluentError Cyclic(Pattern pattern)
        {
            return new($"Cyclic pattern {pattern.Stringify()} detected!", ErrorType.Cyclic);
        }

        public static ResolverFluentError MissingDefault()
        {
            return new("No default", ErrorType.MissingDefault);
        }
    }

    public record ParserFluentError : FluentError
    {
        private readonly ParseError _error;

        private ParserFluentError(ParseError error)
        {
            _error = error;
        }
        
        public static ParserFluentError ParseError(ParseError parseError)
        {
            return new(parseError);
        }

        public override ErrorType ErrorKind()
        {
            return ErrorType.Parser;
        }

        public override string ToString()
        {
            return _error.Message;
        }
    }

    public enum EntryKind : byte
    {
        Message,
        Term,
        Unknown,
    }

    public static class EntryHelper
    {
        public static EntryKind ToKind(this IEntry self)
        {
            if (self is AstTerm)
            {
                return EntryKind.Term;
            }

            return self is AstMessage ? EntryKind.Message : EntryKind.Unknown;
        }
    }

    public enum ErrorType : byte
    {
        Parser,
        Reference,
        Cyclic,
        NoValue,
        TooManyPlaceables,
        Overriding,
        MissingDefault
    }
}