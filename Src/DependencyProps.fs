﻿namespace FsEx.Wpf

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input

module DependencyProps =  


    //---------- creating UIElemnts --------------

    // see: http://www.fssnip.net/4W/title/Calculator
    // http://trelford.com/blog/post/F-operator-overloads-for-WPF-dependency-properties.aspx
    // http://trelford.com/blog/post/Exposing-F-Dynamic-Lookup-to-C-WPF-Silverlight.aspx !!!

    type DependencyPropertyBindingPair(dp:DependencyProperty,binding:Data.BindingBase) =
        member this.Property = dp
        member this.Binding = binding
        static member ( <++> ) (target:#FrameworkElement, pair:DependencyPropertyBindingPair) =
            target.SetBinding(pair.Property,pair.Binding) |> ignore
            target

    type DependencyPropertyValuePair(dp:DependencyProperty,value:obj) =
        member this.Property = dp
        member this.Value = value
        static member ( <+> )  (target:#UIElement, pair:DependencyPropertyValuePair) =
            target.SetValue(pair.Property,pair.Value)
            target

    type Button with
        static member CommandBinding (binding:Data.BindingBase) = 
            DependencyPropertyBindingPair(Button.CommandProperty,binding)

    type Grid with
        static member Column (value:int) =
            DependencyPropertyValuePair(Grid.ColumnProperty,value)
        static member Row (value:int) =
            DependencyPropertyValuePair(Grid.RowProperty,value)

    type TextBox with
        static member TextBinding (binding:Data.BindingBase) =
            DependencyPropertyBindingPair(TextBox.TextProperty,binding)

    let makeGridLength len = new GridLength(len, GridUnitType.Star)

    let makeMenu (xss:list<MenuItem*list<Control>>)=
        let menu = new Menu()
        for h,xs in xss do
            menu.Items.Add (h) |> ignore
            for x in xs do
                h.Items.Add (x) |> ignore            
        menu
    
    let updateMenu (menu:Menu) (xss:list<MenuItem*list<Control>>)=        
        for h,xs in xss do
            menu.Items.Add (h) |> ignore
            for x in xs do
                h.Items.Add (x) |> ignore            
        
    let makeContextMenu (xs:list<#Control>)=
        let menu = new ContextMenu()
        for x in xs do menu.Items.Add (x) |> ignore         
        menu
    
       
    /// clear Grid first and then set with new elements        
    let setGridHorizontal (grid:Grid) (xs:list<UIElement*RowDefinition>)= 
        grid.Children.Clear()
        grid.RowDefinitions.Clear()
        grid.ColumnDefinitions.Clear()
        for i , (e,rd) in List.indexed xs do    
            grid.RowDefinitions.Add (rd)
            grid.Children.Add  ( e <+> Grid.Row i ) |> ignore     
            
    
    /// clear Grid first and then set with new elements
    let setGridVertical (grid:Grid) (xs:list<UIElement*ColumnDefinition>)= 
        grid.Children.Clear()
        grid.RowDefinitions.Clear()
        grid.ColumnDefinitions.Clear()
        for i , (e,cd) in List.indexed xs do    
            grid.ColumnDefinitions.Add (cd)
            grid.Children.Add  ( e <+> Grid.Column i ) |> ignore 
     
    
    let makeGrid (xs:list<UIElement>)= 
        let grid = new Grid()
        for i , e in List.indexed xs do 
            grid.Children.Add  ( e <+> Grid.Row i ) |> ignore  
        grid

    let makePanelVert (xs:list<#UIElement>) =
        let p = new StackPanel(Orientation= Orientation.Vertical)
        for x in xs do
            p.Children.Add x |> ignore
        p
     
    let makePanelHor (xs:list<#UIElement>) =
        let p = new StackPanel(Orientation= Orientation.Horizontal)
        for x in xs do
            p.Children.Add x |> ignore
        p

    let dockPanelVert (top:UIElement, center: UIElement, bottom:UIElement)=
        let d = new DockPanel()
        DockPanel.SetDock(top,Dock.Top)
        DockPanel.SetDock(bottom,Dock.Bottom)
        d.Children.Add(top) |> ignore         
        d.Children.Add(bottom) |> ignore 
        d.Children.Add(center) |> ignore 
        d
    
    
    /// A command that optionally hooks into CommandManager.RequerySuggested to
    /// automatically trigger CanExecuteChanged whenever the CommandManager detects
    /// conditions that might change the output of canExecute. It's necessary to use
    /// this feature for command bindings where the CommandParameter is bound to
    /// another UI control (e.g. a ListView.SelectedItem).
    type Command(execute, canExecute, autoRequery) as this =
        let canExecuteChanged = Event<EventHandler,EventArgs>()
        let handler = EventHandler(fun _ _ -> this.RaiseCanExecuteChanged())
    
        do if autoRequery then CommandManager.RequerySuggested.AddHandler(handler)
        
        member private this._Handler = handler // CommandManager only keeps a weak reference to the event handler, so a strong handler must be maintained
           
        member this.RaiseCanExecuteChanged () = canExecuteChanged.Trigger(this , EventArgs.Empty)        
           
        //interface is implemented as members and as interface members( to be sure it works):
        [<CLIEvent>]
        member this.CanExecuteChanged = canExecuteChanged.Publish
        member this.CanExecute p = canExecute p
        member this.Execute p = execute p
        interface ICommand with
            [<CLIEvent>]
            member this.CanExecuteChanged = this.CanExecuteChanged
            member this.CanExecute p =      this.CanExecute p 
            member this.Execute p =         this.Execute p 
        
    /// creates a ICommand
    let mkCmd canEx ex = new Command(ex,canEx,true) :> ICommand
    
    /// creates a ICommand, CanExecute is always true
    let mkCmdSimple action =
        let ev = Event<_ , _>()
        { new Windows.Input.ICommand with
                [<CLIEvent>]
                member this.CanExecuteChanged = ev.Publish
                member this.CanExecute(obj) = true
                member this.Execute(obj) = action(obj)                
                }

