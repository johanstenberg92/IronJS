# Funkis

[![Stories in Ready](https://badge.waffle.io/johanstenberg92/Funkis.svg?label=ready&title=Ready)](http://waffle.io/johanstenberg92/Funkis)

## Introduction
Funkis is a functional and statically typed language running on the CLR.

Funkis is inspired by:
* OCaml

### Waffle Throughput Graph

[![Throughput Graph](https://graphs.waffle.io/johanstenberg92/Funkis/throughput.svg)](https://waffle.io/johanstenberg92/Funkis/metrics/throughput)

## Language Specification
Initially only an EBNF-grammar will be supported, later maybe a standard library 
will be written.

Comments are the same as in C# - `//` denotes line comments and block comments start
with `/*` and ends with `*/`.

Pattern matching will hopefully be greatly extended in the future.

Inspiration for more advanced pattern matching support can be found [here](http://caml.inria.fr/pub/docs/manual-ocaml/patterns.html).

To read a pointer of a function, just type the name of the function. An example would be if your function's name is `funkis` and has no
arguments, you apply the function by typing `funkis ()` and you read the function pointer through typing `funkis`.


### EBNF-grammar
```
program = { import } ["namespace" property] declaration { declaration }

import = "using" property

declaration =
	"let" (identifier | unit) ":" type = expression
	| "let" "func" identifier [ parameters ] ":" type = expression

expression =
	list_declaration
	| tuple_declaration
	| "match" expression pattern_catch { pattern_catch }
	| "if" expression "then" expression "else" expression
	| "func" [ parameters ] ":" type "->" expression
	| "let" identifier ":" type "=" expression "in" expression
    | ["+" | "-"] term

parameters = "(" identifier ":" type { "," identifier ":" type } ")"

type =
	property
	| unit
	| (unit | "(" type { "," type } ")" ) "->" type

list_declaration = "[" [ expression { "," expression } ] "]"

tuple_declaration = "(" expression "," expression { "," expression } ")"

pattern_catch = "|" pattern "->" expression

pattern = literal

term = factor { ("*" | "/" | "+" | "-" | "==" | ">=" | "<=" | "!=" | "<" | ">" | "||" | "&&" | "::" |  ) factor }

factor =
	property
    | property "(" expression { "," expression } ")"
	| "(" expression ")"
    | literal
	
property = identifier { "." identifier }

literal =
    int
	| long
	| float
	| double
	| bool
	| char
	| string
	| unit

int = digit { digit }

long = digit { digit } "L"

double = digit { digit } "." digit { digit }

float = digit { digit } "." digit { digit } "F"

bool = ("true" | "false")

char = "'" <TODO> "'"

string = """ <TODO> """

unit = "()"

identifier = (upper_case_letter | lower_case_letter) { (upper_case_letter | lower_case_letter | digit | "_") }

newline = "\n" | "\r\n"

upper_case_letter = ("A" | "B" | "C" | "D" | "E" | "F" | "G" | "H" | "I" | "J" | "K" | "L" | "M" | "N" | "O" | "P" | "Q" | "R" | "S" | "T" | "U" | "V" | "W" | "X" | "Y" | "Z")

lower_case_letter = ("a" | "b" | "c" | "d" | "e" | "f" | "g" | "h" | "i" | "j" | "k" | "l" | "m" | "n" | "o" | "p" | "q" | "r" | "s" | "t" | "u" | "v" | "w" | "x" | "y" | "z")

digit = ("0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9")
```

## Examples
Examples of Funkis programs can be found in the `FunkisTests/TestFiles` folder.

### Hello World
```
let func hello_world : () = 
    println ("hello world!")

let () : () = hello_world ()
```