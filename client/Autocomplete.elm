module Autocomplete exposing (..)

-- builtin
import Dict
import Json.Decode as JSD
import Dom.Scroll
import Task

-- lib
import List.Extra as LE
-- import Maybe.Extra as ME
-- import String.Extra as SE

-- dark
import Util exposing (deMaybe, int2letter, letter2int)
import Types exposing (..)
import Runtime as RT

-- show the prev 5
-- obvi this should use getClientBoundingBox, but that's tough in Elm
height : Int -> Int
height i = if i < 5
           then 0
           else 16 * (i - 5)

focusItem : Int -> Cmd Msg
focusItem i = Dom.Scroll.toY "autocomplete-holder" (i |> height |> toFloat)
            |> Task.attempt FocusAutocompleteItem

empty : Autocomplete
empty = init []

init : List Function -> Autocomplete
init functions = { functions = functions
                 , varnames = []
                 , completions = List.map ACFunction functions
                 , index = -1
                 , open = True
                 , showFunctions = True
                 , value = ""
                 , liveValue = Nothing
                 , tipe = Nothing
                 }

forLiveValue : Maybe LiveValue -> Autocomplete -> Autocomplete
forLiveValue lv a = { a | liveValue = lv }

-- forParamType : Tipe -> NodeList -> Autocomplete -> Autocomplete
-- forParamType tipe ns a = { a | tipe = Just tipe, nodes = Just ns }
--
reset : Autocomplete -> Autocomplete
reset a = init a.functions

clear : Autocomplete -> Autocomplete
clear a = let cleared = setQuery "" a in
          { cleared | index = -1 }

open : Bool -> Autocomplete -> Autocomplete
open o a = { a | open = o }

complete : String -> Autocomplete -> Autocomplete
complete str a = { a | value = str
                     , completions = []
                     , index = -1
                     , open = True
                 }

selectDown : Autocomplete -> Autocomplete
selectDown a = let max_ = List.length a.completions
                   max = Basics.max max_ 1
                   new = (a.index + 1) % max
               in
                 { a | index = new }

selectUp : Autocomplete -> Autocomplete
selectUp a = let max = List.length a.completions - 1 in
             { a | index = if a.index <= 0
                           then max
                           else a.index - 1
             }

highlighted : Autocomplete -> Maybe AutocompleteItem
highlighted a = LE.getAt a.index a.completions

sharedPrefix2 : String -> String -> String
sharedPrefix2 l r =
  case (String.uncons l, String.uncons r) of
    (Just (l1, lrest), Just (r1, rrest)) ->
      if l1 == r1 then
        String.fromChar l1 ++ sharedPrefix2 lrest rrest
      else
        ""
    _ -> ""

sharedPrefixList : List String -> String
sharedPrefixList strs =
  case List.head strs of
    Nothing -> ""
    Just s -> List.foldl sharedPrefix2 s strs

-- Find the shared prefix of all the possible suggestions (eg "List::")
sharedPrefix : Autocomplete -> String
sharedPrefix a = sharedPrefixList (List.map asName a.completions)

-- returns (indent, suggestion, search), where:
--  - indent is the string that occurs before the match
--  - suggestion is the match rewritten with the search
--  - search is the search rewritten to match the suggestion
compareSuggestionWithActual : Autocomplete -> String -> (String, String, String)
compareSuggestionWithActual a actual =
  let suggestion = sharedPrefix a
  in
    case String.indexes (String.toLower actual) (String.toLower suggestion) of
      [] -> ("", suggestion, actual)
      index :: _ ->
        let prefix = String.slice 0 index suggestion
            suffix = String.slice (index + String.length actual) (String.length suggestion) suggestion
        in
           (prefix, prefix ++ actual ++ suffix, actual)

asName : AutocompleteItem -> String
asName aci =
  case aci of
    ACFunction {name} -> name
    ACField name -> name
    ACVariable name -> name

asTypeString : AutocompleteItem -> String
asTypeString item =
  case item of
    ACFunction f -> f.parameters
                    |> List.map .tipe
                    |> List.map RT.tipe2str
                    |> String.join ", "
                    |> (\s -> "(" ++ s ++ ") ->  " ++ (RT.tipe2str f.returnTipe))
    ACField _ -> ""
    ACVariable _ -> ""

asString : AutocompleteItem -> String
asString aci =
  asName aci ++ asTypeString aci

containsOrdered : String -> String -> Bool
containsOrdered needle haystack =
  case String.uncons needle of
    Just (c, newneedle) ->
      let char = String.fromChar c in
      String.contains char haystack
        && containsOrdered newneedle (haystack
                                        |> String.split char
                                        |> List.drop 1
                                        |> String.join char)
    Nothing -> True

-- parse the json, take the list of keys, add a . to the front of it
-- TODO: this needs refactoring
jsonFields : String -> List AutocompleteItem
jsonFields json =
  json
    |> JSD.decodeString (JSD.dict JSD.value)
    |> Result.toMaybe
    |> deMaybe
    |> Dict.keys
    |> List.map ACField

-- variablesForType : NodeList -> Tipe -> List AutocompleteItem
-- variablesForType ns t =
--     ns
--       |> List.indexedMap (,)
--       |> List.filter (\(_, n) -> RT.isCompatible t n.liveValue.tipe)
--       |> List.map (\(i, n) -> (int2letter i, n))
--       |> List.map ACVariable
--
-- Implementation:
-- n The autocomplete list should include:
--    y all imported functions
--    y restricted by types that are allowed
--    y allowed field names
--    n library names
--    y case-insensitive
-- n order by most likely, offer other alternatives below
--   n slight typos
--   n slight typeos
-- y Press enter to select
-- y Press right to fill as much as is definitive

setQuery : String -> Autocomplete -> Autocomplete
setQuery q a = { a | value = q } |> regenerate

appendQuery : String -> Autocomplete -> Autocomplete
appendQuery str a =
  let q = if isStringEntry a |> Debug.log "isstring"
          then String.dropRight 1 a.value ++ str ++ "\""
          else a.value ++ str
      _ = Debug.log "str" str
  in setQuery (Debug.log "q" q) a


regenerate : Autocomplete -> Autocomplete
regenerate a =
  let lcq = String.toLower a.value
      -- fields of objects
      fields = case a.liveValue of
                 Just lv -> if lv.tipe == TObj
                            then jsonFields lv.json
                            else []
                 Nothing -> []
      --
      -- variables
      -- variables = case (a.tipe, a.nodes) of
      --                 (Just t, Just ns)  -> variablesForType ns t
      --                 _ -> []

      -- functions
      functions = if a.showFunctions then a.functions else []
      options =
        functions
        |> List.filter
           (\{returnTipe} ->
              case a.tipe of
                Just t -> RT.isCompatible returnTipe t
                Nothing -> True)
        |> List.filter
          (\fn ->
             case a.liveValue of
               Just {tipe} -> Nothing /= findParamByType fn tipe
               Nothing -> True)
        |> List.map ACFunction
        |> List.append fields
        |> List.append (List.map ACVariable a.varnames)
        |> List.filter
           (\i -> i
                  |> (\i -> if 1 >= String.length lcq
                            then asName i
                            else asString i)
                  |> String.toLower
                  |> Util.replace "⟶" "->"
                  |> String.contains lcq)

      completions =
        if not a.open
        then []
        else options

  in { a | completions = completions
         , index = if List.length completions == 0
                   then -1
                   else if List.length completions < List.length a.completions
                   then 0
                   else a.index
     }

setVarnames : List VarName -> Autocomplete -> Autocomplete
setVarnames vs a =
  { a | varnames = vs }

showFunctions : Bool -> Autocomplete -> Autocomplete
showFunctions b a =
  { a | showFunctions = b }

update : AutocompleteMod -> Autocomplete -> Autocomplete
update mod a =
  (case mod of
     ACSetQuery str -> setQuery str a
     ACAppendQuery str -> appendQuery str a
     ACReset -> reset a
     ACOpen o -> open o a
     ACClear -> clear a
     ACComplete str -> complete str a
     ACSelectDown -> selectDown a
     ACSelectUp -> selectUp a
     ACFilterByLiveValue lv -> forLiveValue lv a
     ACSetAvailableVarnames vs -> setVarnames vs a
     ACShowFunctions bool -> showFunctions bool a
     -- ACFilterByParamType tipe nodes -> forParamType tipe nodes a
  )
  |> regenerate



findParamByType : Function -> Tipe -> Maybe Parameter
findParamByType {parameters} tipe =
  parameters
  |> LE.find (\p -> RT.isCompatible p.tipe tipe)

findFirstParam : Function -> Maybe Parameter -> Maybe Parameter
findFirstParam {parameters} except =
  LE.find (\p -> Just p /= except) parameters

findFunction : Autocomplete -> String -> Maybe Function
findFunction a name =
  LE.find (\f -> f.name == name) a.functions

isStringEntry : Autocomplete -> Bool
isStringEntry a = String.startsWith "\"" a.value

isSmallStringEntry : Autocomplete -> Bool
isSmallStringEntry a =
  isStringEntry a && not (isLargeStringEntry a)

isLargeStringEntry : Autocomplete -> Bool
isLargeStringEntry a =
  isStringEntry a && String.contains "\n" a.value
