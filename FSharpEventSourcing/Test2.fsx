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

    let read<'a> dir topic : 'a array option =
        let file = Path.Combine(dir, topic + ".json")
        if File.Exists(file) then
            let text = File.ReadAllText(file)
            Json.deserialize<'a array>(text) |> Some
        else
            None

    let readAfter<'a> dir topic (i : int) =
        read<'a> dir topic
        |> Option.map (fun e -> e.[i..])
        
    let write<'a> dir topic e =
        let writeEvents file events =
            let data = Json.serialize events
            File.WriteAllText(file, data)

        if not(Directory.Exists(dir)) then
            Directory.CreateDirectory(dir) |> ignore

        let file = Path.Combine(dir, topic + ".json")

        let events = read<'a> dir topic
        match events with
        | Some ev -> 
            Array.append ev [|e|]
            |> writeEvents file
        | None ->
            writeEvents file [|e|]


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

    let private init = {
        Id = Guid.Parse "00000000-0000-0000-0000-000000000000"
        Lines = []
    }

    module Events =
        type Created = {
            Id : Guid
        }

        type LineAdded = {
            LineId : int
            ItemId : string
            Amount : decimal
        }

        type LineRemoved = {
            LineId : int
        }

        type Event =
        | Created of Created
        | LineAdded of LineAdded
        | LineRemoved of LineRemoved

        let private create (order : Order) (c : Created) =
            { order with Id = c.Id }

        let private addLine (order : Order) (a : LineAdded) =
            {order with Lines = order.Lines @ [{LineId = a.LineId; ItemId = a.ItemId; Amount = a.Amount}]}

        let private removeLine (order : Order) (r : LineRemoved) =
            let newLines = order.Lines |> List.where (fun l -> l.LineId <> r.LineId)
            {order with Lines = newLines}

        let map (order : Order) (e : Event) =
            match e with
            | Created c -> create order c
            | LineAdded a -> addLine order a
            | LineRemoved r -> removeLine order r

    type Querier = string -> Order option

    let queryBuilder (eventSource : string -> Events.Event array option) : Querier =
        fun (id : string) -> 
            eventSource id
            |> Option.map (Array.fold (Events.map) (init))

    module Commands =
        open Events
        // This is where business logic would be
        type CreateOrder = {
            Id : Guid
            Request : Events.Created
        }

        type AddLine = {
            Id : Guid
            Request : Events.LineAdded
        }

        type RemoveLine = {
            Id : Guid
            Request : Events.LineRemoved
        }

        let createOrder (q : Querier) (c : CreateOrder) =
            let order = q c.Id

//#region scratchpad       
let eventDBDir = @".\EventDB"
let reader<'a> = EventDB.read<'a> eventDBDir
let readAfter<'a> = EventDB.readAfter<'a> eventDBDir
let writer<'a> = EventDB.write<'a> eventDBDir

let orderId = "a0000000-0000-0000-0000-000000000000"

// let e1 = Order.Events.Event.Create {
//     Id = Guid.Parse orderId
// }

// let e2 = Order.Events.Event.AddLine {
//     LineId = 1
//     ItemId = "a"
//     Amount = 10M
// }

// let e3 = Order.Events.Event.AddLine {
//     LineId = 2
//     ItemId = "chicken"
//     Amount = 5M
// }

// [|e1; e2; e3|]
// |> Array.iter (fun e -> writer orderId e)

let orderQuery = Order.queryBuilder reader
let order = orderQuery orderId

//#endregion