/// Payloads that we send to the client via Pusher.com
module ClientTypes.Pusher

open Prelude

module Payload =
  type NewTrace = Analysis.TraceID * tlid list

  type NewStaticDeploy =
    { deployHash : string
      url : string
      lastUpdate : NodaTime.Instant
      status : StaticDeploy.DeployStatus }

  type New404 = string * string * string * NodaTime.Instant * Analysis.TraceID
