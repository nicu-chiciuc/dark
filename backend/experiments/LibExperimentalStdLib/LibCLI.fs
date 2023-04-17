module LibExperimentalStdLib.LibCLI

open System.Threading.Tasks
open FSharp.Control.Tasks

open Prelude
open LibExecution.RuntimeTypes

module Errors = LibExecution.Errors

let fn = FQFnName.stdlibFnName

let err (str : string) = Ply(Dval.errStr str)

let incorrectArgs = Errors.incorrectArgs

let varA = TVariable "a"


let fns : List<BuiltInFn> =
  [ { name = fn "File" "read" 0
      typeParams = []
      parameters = [ Param.make "path" TString "" ]
      returnType = TResult(TBytes, TString)
      description =
        "Reads the contents of a file specified by <param path> asynchronously and returns its contents as Bytes wrapped in a Result"
      fn =
        (function
        | _, _, [ DString path ] ->
          uply {
            try
              let! contents = System.IO.File.ReadAllBytesAsync path
              return DResult(Ok(DBytes contents))
            with
            | e -> return DResult(Error(DString($"Error reading file: {e.Message}")))
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }


    { name = fn "Directory" "pwd" 0
      typeParams = []
      parameters = []
      returnType = TString
      description = "Returns the current working directory"
      fn =
        (function
        | _, _, [] ->
          uply {
            let contents = System.IO.Directory.GetCurrentDirectory()
            return DString contents
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }


    { name = fn "Directory" "ls" 0
      typeParams = []
      parameters = [ Param.make "path" TString "" ]
      returnType = TList TString
      description = "Returns the current working directory"
      fn =
        (function
        | _, _, [ DString path ] ->
          uply {
            let contents = System.IO.Directory.EnumerateFiles path |> Seq.toList
            return List.map DString contents |> DList
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }


    { name = fn "EnvVar" "get" 0
      typeParams = []
      parameters = [ Param.make "varName" TString "" ]
      returnType = TOption TString
      description =
        "Gets the value of the environment variable with the given <param varName> if it exists."
      fn =
        (function
        | _, _, [ DString varName ] ->
          let envValue = System.Environment.GetEnvironmentVariable(varName)

          if isNull envValue then
            Ply(DOption None)
          else
            Ply(DOption(Some(DString envValue)))
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }


    { name = fn "EnvVar" "getAll" 0
      typeParams = []
      parameters = []
      returnType = TDict TString
      description =
        "Returns a list of tuples containing all the environment variables and their values."
      fn =
        (function
        | _, _, [] ->
          let envVars = System.Environment.GetEnvironmentVariables()

          let envMap =
            envVars
            |> Seq.cast<System.Collections.DictionaryEntry>
            |> Seq.map (fun kv -> (string kv.Key, DString(string kv.Value)))
            |> Map.ofSeq
            |> DDict

          Ply(envMap)
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }



    { name = fn "File" "write" 0
      typeParams = []
      parameters = [ Param.make "path" TString ""; Param.make "contents" TBytes "" ]
      returnType = TResult(TUnit, TString)
      description =
        "Writes the specified byte array <param contents> to the file specified by <param path> asynchronously"
      fn =
        (function
        | _, _, [ DString path; DBytes contents ] ->
          uply {
            try
              do! System.IO.File.WriteAllBytesAsync(path, contents)
              return DResult(Ok(DUnit))
            with
            | e -> return DResult(Error(DString($"Error writing file: {e.Message}")))
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }


    { name = fn "File" "appendText" 0
      typeParams = []
      parameters = [ Param.make "path" TString ""; Param.make "contents" TString "" ]
      returnType = TResult(TUnit, TString)
      description =
        "Appends the specified text <param contents> to the file specified by <param path> asynchronously"
      fn =
        (function
        | _, _, [ DString path; DString contents ] ->
          uply {
            try
              do! System.IO.File.AppendAllTextAsync(path, contents)
              return DResult(Ok(DUnit))
            with
            | e ->
              return DResult(Error(DString($"Error appending to file: {e.Message}")))
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }



    ]
