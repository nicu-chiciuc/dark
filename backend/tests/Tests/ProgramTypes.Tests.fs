module Tests.ProgramTypes

open Expecto
open Prelude
open TestUtils.TestUtils

module ST = LibBinarySerialization.SerializedTypes
module PT = LibExecution.ProgramTypes
module RT = LibExecution.RuntimeTypes
module ST2PT = LibBinarySerialization.SerializedTypesToProgramTypes
module PT2ST = LibBinarySerialization.ProgramTypesToSerializedTypes
module PT2RT = LibExecution.ProgramTypesToRuntimeTypes
module PTParser = LibExecution.ProgramTypesParser
module S = TestUtils.RTShortcuts

let ptFQFnName =
  testMany
    "ProgramTypes.FQFnName.ToString"
    (fun name -> name |> PT2RT.FQFnName.toRT |> RT.FQFnName.toString)
    [ (PT.FQFnName.stdlibFqName "String" "append" 1), "String::append_v1" ]


let testPipesToRuntimeTypes =
  test "pipes to runtime types" {
    let actual = Parser.parseRTExpr "value.age |> (-) 2 |> (+) value.age |> (<) 3"

    let expected =
      S.ePipeApply
        (S.eStdFnVal "Int" "lessThan" 0)
        [ S.ePipeApply
            (S.eStdFnVal "Int" "add" 0)
            [ S.ePipeApply
                (S.eStdFnVal "Int" "subtract" 0)
                [ S.eFieldAccess (S.eVar "value") "age"; S.eInt 2 ]
              S.eFieldAccess (S.eVar "value") "age" ]
          S.eInt 3 ]

    Expect.equalExprIgnoringIDs actual expected
  }

let testProgramTypesToRuntimeTypes =
  let u = PT.EUnit(8UL)
  let ru = RT.EUnit(8UL)
  testMany
    "program types to runtime types"
    PT2RT.Expr.toRT
    [ PT.EFloat(7UL, Positive, "", "0"), RT.EFloat(7UL, 0.0)
      PT.EFloat(7UL, Positive, "0", ""), RT.EFloat(7UL, 0.0)
      PT.EFloat(7UL, Positive, "", ""), RT.EFloat(7UL, 0.0)
      (PT.EMatch(9UL, u, [ PT.MPFloat(5UL, Positive, "", ""), u ]),
       RT.EMatch(9UL, ru, [ RT.MPFloat(5UL, 0.0), ru ]))
      (PT.EMatch(9UL, u, [ PT.MPFloat(5UL, Positive, "0", ""), u ]),
       RT.EMatch(9UL, ru, [ RT.MPFloat(5UL, 0.0), ru ]))
      (PT.EMatch(9UL, u, [ PT.MPFloat(5UL, Positive, "", "0"), u ]),
       RT.EMatch(9UL, ru, [ RT.MPFloat(5UL, 0.0), ru ])) ]

let testInfixProgramTypesToSerializedTypes =
  testMany
    "infix program types to serialized types"
    PT2ST.Expr.toST
    [ (PT.EInfix(
        8UL,
        PT.InfixFnCall(PT.ArithmeticPlus),
        PT.EInteger(9UL, 6),
        PT.EInteger(10UL, 6)
       ),
       ST.EInfix(
         8UL,
         ST.InfixFnCall(ST.ArithmeticPlus),
         ST.EInteger(9UL, 6),
         ST.EInteger(10UL, 6)
       )) ]


let tests =
  testList
    "ProgramTypes"
    [ testPipesToRuntimeTypes
      testProgramTypesToRuntimeTypes
      ptFQFnName
      testInfixProgramTypesToSerializedTypes ]
