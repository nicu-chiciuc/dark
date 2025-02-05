/// Module to work with Runtime ASTs
module LibExecution.RuntimeTypesAst

open System.Threading.Tasks
open FSharp.Control.Tasks

open Prelude
open VendoredTablecloth
open RuntimeTypes

let rec preTraversal (f : Expr -> Expr) (expr : Expr) : Expr =
  let r = preTraversal f
  let expr = f expr

  match expr with
  | EInteger _
  | EString _
  | EVariable _
  | EBool _
  | EUnit _
  | ECharacter _
  | EFQFnValue _
  | EFloat _ -> expr
  | ELet (id, pat, rhs, next) -> ELet(id, pat, r rhs, r next)
  | EIf (id, cond, ifexpr, elseexpr) -> EIf(id, r cond, r ifexpr, r elseexpr)
  | EFieldAccess (id, expr, fieldname) -> EFieldAccess(id, r expr, fieldname)
  | EApply (id, name, exprs, inPipe) -> EApply(id, name, List.map r exprs, inPipe)
  | ELambda (id, names, expr) -> ELambda(id, names, r expr)
  | EList (id, exprs) -> EList(id, List.map r exprs)
  | ETuple (id, first, second, theRest) ->
    ETuple(id, r first, r second, List.map r theRest)
  | EMatch (id, mexpr, pairs) ->
    EMatch(id, r mexpr, List.map (fun (name, expr) -> (name, r expr)) pairs)
  | ERecord (id, fields) ->
    ERecord(id, List.map (fun (name, expr) -> (name, r expr)) fields)
  | EConstructor (id, name, exprs) -> EConstructor(id, name, List.map r exprs)
  | EFeatureFlag (id, cond, casea, caseb) ->
    EFeatureFlag(id, r cond, r casea, r caseb)
  | EAnd (id, left, right) -> EAnd(id, r left, r right)
  | EOr (id, left, right) -> EOr(id, r left, r right)

let rec postTraversal (f : Expr -> Expr) (expr : Expr) : Expr =
  let r = postTraversal f

  let result =
    match expr with
    | EInteger _
    | EString _
    | EVariable _
    | ECharacter _
    | EFQFnValue _
    | EBool _
    | EUnit _
    | EFloat _ -> expr
    | ELet (id, pat, rhs, next) -> ELet(id, pat, r rhs, r next)
    | EApply (id, name, exprs, inPipe) -> EApply(id, name, List.map r exprs, inPipe)
    | EIf (id, cond, ifexpr, elseexpr) -> EIf(id, r cond, r ifexpr, r elseexpr)
    | EFieldAccess (id, expr, fieldname) -> EFieldAccess(id, r expr, fieldname)
    | ELambda (id, names, expr) -> ELambda(id, names, r expr)
    | EList (id, exprs) -> EList(id, List.map r exprs)
    | ETuple (id, first, second, theRest) ->
      ETuple(id, r first, r second, List.map r theRest)
    | EMatch (id, mexpr, pairs) ->
      EMatch(id, r mexpr, List.map (fun (name, expr) -> (name, r expr)) pairs)
    | ERecord (id, fields) ->
      ERecord(id, List.map (fun (name, expr) -> (name, r expr)) fields)
    | EConstructor (id, name, exprs) -> EConstructor(id, name, List.map r exprs)
    | EFeatureFlag (id, cond, casea, caseb) ->
      EFeatureFlag(id, r cond, r casea, r caseb)
    | EAnd (id, left, right) -> EAnd(id, r left, r right)
    | EOr (id, left, right) -> EOr(id, r left, r right)

  f result
