﻿namespace Fittings

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input

/// A module to provide DependencyProperties and their bindings
/// Includes extension methods for Button, Grid, TextBox
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
        d.Children.Add(center) |> ignore // add the element to claim all the space last
        d





