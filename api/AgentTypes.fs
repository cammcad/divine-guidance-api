namespace Api


type MsgIPA = Increment of int | Fetch of AsyncReplyChannel<int> | StopIPA

exception IPAStopException
