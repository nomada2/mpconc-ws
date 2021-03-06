﻿module MainApp

open System
open System.Windows
open System.Windows.Controls

open FSharpx

type MementoMessage =
    | Put of Point
    | Get of AsyncReplyChannel<Point list option>

type MainWindow = XAML<"MainWindow.xaml">

type DragState = {
    /// Are we currently dragging?
    dragging : bool;
    /// Current position of the drag
    position : Point;
    /// Drag offset, for taking into account initial drag position
    offset : Point }

/// A type of change that can happen to the DragState
type DragChange =
    /// Start dragging from a certain offset
    | StartDrag of Point
    /// Stop dragging
    | StopDrag
    /// Update the drag with a new position
    | UpdatePosition of Point


/// Initialized the application
let loadWindow() =
    let window = MainWindow()

    /// StartDrag events
    let start_drag =
        window.Ball.MouseDown
        |> Observable.filter (fun btn -> btn.ChangedButton = Input.MouseButton.Left)
        |> Observable.map (fun x -> StartDrag(x.GetPosition(window.Ball)))

    /// StopDrag events
    let stop_drag =
        window.Canvas.MouseUp
        |> Observable.filter(fun btn -> btn.ChangedButton = Input.MouseButton.Left)
        |> Observable.map (fun _ -> StopDrag)

    /// UpdatePosition events
    let moving =
        window.Canvas.MouseMove
        |> Observable.map (fun x -> UpdatePosition(x.GetPosition(window.Canvas)))

    // TODO : 3.3
    // create an agent that maintains an internal state with the Point(s) of the mouse
    // when the circle is dragged
    // the agent should return the current state when requested (see MementoMessage type)
    let agentMemento =
        MailboxProcessor<MementoMessage>.Start(fun inbox ->
                // replace the following line of code with your implementation
                async.Return())


    let ballXoffest = window.Ball.Width / 2.
    let ballYoffest = window.Ball.Height / 2.

    let moveBall (position:Point) =
        Canvas.SetLeft(window.Ball, position.X)
        Canvas.SetTop(window.Ball, position.Y)

    /// Subscription for the entire Drag command
    let subscription =

        // TODO 3.4
        // TODO :   Collect the coordinate points during the dragging,
        //          Create an undo logic (memento pattern), when the mouse is released
        //          the ball goes backward (adding a delay for animation)
        //          follow the original path until the original starting point is reached

        // TODO
        // merge the start_drag observable with  stop_drag and moving observables
        start_drag
        |> Observable.scan (fun (state : DragState) (change : DragChange) ->
                            match change with
                            | StartDrag(offset) -> { state with dragging=true; offset=offset }
                            | StopDrag ->       let getPoints =
                                                     agentMemento.PostAndReply(fun ch -> Get(ch))
                                                match getPoints with
                                                | None -> ()
                                                | Some(points) ->
                                                    async { for p in points do
                                                                do! Async.Sleep 50
                                                                window.polyline.Points.Remove(p) |> ignore
                                                                moveBall p } |> Async.StartImmediate
                                                { state with position=new Point(); dragging=false}
                            | UpdatePosition(pos) ->  if state.dragging = true
                                                            then { state with position=pos }
                                                      else state)
            { dragging=false; position=new Point(); offset=new Point() }
        // TODO Filter only events when state is dragging
        // code here
        |> Observable.map (fun (state : DragState) ->
                    let diff = state.position - state.offset
                    Point(diff.X, diff.Y))
        |> Observable.subscribe (fun (position : Point) ->
                    moveBall(position)
                    window.polyline.Points.Add(position))

    window.Root

[<STAThread>]
(new Application()).Run(loadWindow()) |> ignore

