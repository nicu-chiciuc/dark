val focusItem : int -> Types.msg Tea.Cmd.t

type t = Types.fluidAutocompleteState [@@deriving show]

type item = Types.fluidAutocompleteItem [@@deriving show]

type data = Types.fluidAutocompleteData [@@deriving show]

type query = TLID.t * Types.fluidTokenInfo [@@deriving show]

val asName : item -> string

val asTypeStrings : item -> string list * string

val isVariable : item -> bool

val isField : item -> bool

val item : data -> item

val allFunctions : Types.model -> Types.function_ list

val highlightedWithValidity : t -> data option

val highlighted : t -> item option

val reset : Types.model -> t

val init : Types.model -> t

val regenerate : Types.model -> t -> TLID.t * Types.fluidTokenInfo -> t

val updateFunctions : Types.model -> Types.model

val numCompletions : t -> int

val selectUp : t -> t

val selectDown : t -> t

val documentationForItem :
  Types.fluidAutocompleteData -> Types.msg Vdom.t list option

val isOpened : t -> bool

val updateAutocompleteVisibility : Types.model -> Types.model

(* only exposed for tests *)
type fullQuery =
  { tl : Types.toplevel
  ; ti : Types.fluidTokenInfo
  ; fieldList : string list
  ; pipedDval : Types.dval option
  ; queryString : string }

val refilter : fullQuery -> t -> item list -> t
