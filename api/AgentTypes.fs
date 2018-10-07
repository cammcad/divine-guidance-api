namespace Api
open System


type AngelNumber() =
  member val Id = 0 with get,set
  member val Number = 0 with get,set
  member val Info = String.Empty with get,set

//type MsgIPA = Increment of int | Fetch of AsyncReplyChannel<int> | StopIPA
type MsgAngelNumberReq = Fetch of int * AsyncReplyChannel<AngelNumber> | StopAngelNumberAgent

exception AngelNumberReqStopException
