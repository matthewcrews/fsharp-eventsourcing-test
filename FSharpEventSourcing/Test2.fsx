#I __SOURCE_DIRECTORY__
#I "../packages/Newtonsoft.Json/lib/net45"
#r "Newtonsoft.Json.dll"

open System
open System.IO

module EventDB =
    module Json = 
        open Newtonsoft.Json 
        let serialize obj = 
            JsonConvert.SerializeObject(obj, Formatting.Indented)
        let deserialize<'a> str = 
            JsonConvert.DeserializeObject<'a> str

    let read<'a> dir topic : 'a array =
        let file = Path.Combine(dir, topic + ".json")
        let text = File.ReadAllText(file)
        Json.deserialize<'a array>(text)

    let readAfter<'a> dir topic (i : int) =
        (read<'a> dir topic).[i..]
        
    let write<'a> dir topic e =
        let writeEvents file events =
            let data = Json.serialize events
            File.WriteAllText(file, data)

        if not(Directory.Exists(dir)) then
            Directory.CreateDirectory(dir) |> ignore

        let file = Path.Combine(dir, topic + ".json")

        if not(File.Exists(file)) then
            [|e|]
            |> writeEvents file
        else
            let events = read<'a> dir topic
            Array.append events [|e|]
            |> writeEvents file


module Order =
    type OrderLine = {
        LineId : int
        ItemId : string
        Amount : decimal
    }

    type Order = {
        Id : Guid
        Lines : OrderLine list
    }

    let init = {
        Id = Guid.Parse "00000000-0000-0000-0000-000000000000"
        Lines = []
    }

    module Events =
        type Create = {
            Id : Guid
        }

        type AddLine = {
            LineId : int
            ItemId : string
            Amount : decimal
        }

        type RemoveLine = {
            LineId : int
        }

        type Event =
        | Create of Create
        | AddLine of AddLine
        | RemoveLine of RemoveLine

        let private create (order : Order) (c : Create) =
            { order with Id = c.Id }

        let private addLine (order : Order) (a : AddLine) =
            {order with Lines = order.Lines @ [{LineId = a.LineId; ItemId = a.ItemId; Amount = a.Amount}]}

        let private removeLine (order : Order) (r : RemoveLine) =
            let newLines = order.Lines |> List.where (fun l -> l.LineId <> r.LineId)
            {order with Lines = newLines}

        let map (order : Order) (e : Event) =
            match e with
            | Create c -> create order c
            | AddLine a -> addLine order a
            | RemoveLine r -> removeLine order r

    let queryBuilder (eventSource : string -> Events.Event array) =
        fun (id : string) -> 
            eventSource id
            |> Array.fold (Events.map) (init)
        
let eventDBDir = @".\EventDB"
let reader<'a> = EventDB.read<'a> eventDBDir
let readAfter<'a> = EventDB.readAfter<'a> eventDBDir
let writer<'a> = EventDB.write<'a> eventDBDir

let orderId = "a0000000-0000-0000-0000-000000000000"

let e1 = Order.Events.Event.Create {
    Id = Guid.Parse orderId
}

let e2 = Order.Events.Event.AddLine {
    LineId = 1
    ItemId = "a"
    Amount = 10M
}

let e3 = Order.Events.Event.AddLine {
    LineId = 2
    ItemId = "chicken"
    Amount = 5M
}

[|e1; e2; e3|]
|> Array.iter (fun e -> writer orderId e)

let orderQuery = Order.queryBuilder reader<Order.Events.Event>
let order = orderQuery orderId