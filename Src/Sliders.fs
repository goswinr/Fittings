namespace FsEx.Wpf

open System
open System.Windows

open System.Windows.Controls

open System.Windows.Input
open FsEx.Wpf.ViewModel
open FsEx.Wpf.AutoPrecicionFloats
open FsEx.Wpf.ManualPrecicionFloats
open System.Windows.Controls.Primitives // for Thumb



       
module Sliders =
    
    
    (* doesnt work, even in window on loaded event
    /// only works after slider has been rendered
    let getThumb(slider:Slider)=        
        //https://stackoverflow.com/questions/3233000/get-the-thumb-of-a-slider/52066877
        let track = slider.Template.FindName("PART_Track", slider) 
        if  track <> null then Some (track:?> Track).Thumb 
        else None

    let th :Thumb  = // fails too?
        fun i -> VisualTreeHelper.GetChild(levelsSlider.slider , i)
        |> Seq.init (VisualTreeHelper.GetChildrenCount(levelsSlider.slider)) 
        |> Seq.find ( fun e -> e :? Thumb) 
        :?> Thumb
    *)

    /// a silder with some more usefull events
    type SliderEx () as this  =    
        inherit Slider()
        
        let mutable isDragging = false

        let nonDrag = new Event<float>() 
        let dragStop = new Event<float>() 
        
        do 
            this.AddHandler(Thumb.DragStartedEvent,    new DragStartedEventHandler( fun _ _  -> isDragging <- true )) 

            this.AddHandler(Thumb.DragCompletedEvent,  new DragCompletedEventHandler( fun _ _ -> 
                isDragging <- false
                dragStop.Trigger(this.Value) ))

            this.ValueChanged.Add (fun v -> if not isDragging then nonDrag.Trigger(v.NewValue))

           
        /// a slider Value chnaged event that only aoocures aon non thumbdragging events
        [<CLIEvent>]
        member x.NonDragChange = nonDrag.Publish

        /// when moving the thumb stops. mouse is release
        [<CLIEvent>]
        member x.DragOfThumbCompleted = dragStop.Publish            

    
    /// add a element to a UIElementCollection
    let inline add (el:#UIElement) (parent:UIElementCollection) = parent.Add el  |> ignore ; parent
    
    

    /// A View model for a slider control
    /// Includes a changed event if value changes
    type AutoPrecicionSliderViewModel (minVal:float, initalVal:float, maxVal:float) as this = 
        inherit ViewModelBase()
    
        let changed = new Event<float>()        
        let minChanged = new Event<float>()        
        let maxChanged = new Event<float>()        
    
        let mutable minVal = minVal
        let mutable maxVal = maxVal
        let mutable currentVal = initalVal 
        //let currentVal = ref initalVal 
    
        member this.MinVal
            with get()  = minVal
            and set(v) = 
                if v<>minVal then  
                    minVal <- v
                    minChanged.Trigger(v) // do first ?
                    this.OnPropertyChanged(nameof this.MinVal)
        
         member this.MaxVal
            with get()  = maxVal
            and set(v) =
                if v<>maxVal then 
                    maxVal <- v
                    maxChanged.Trigger(v) // do first ?
                    this.OnPropertyChanged(nameof this.MaxVal)     

        member this.CurrentValue
            with get()  = currentVal
            and set(v) =
                if v <> currentVal then 
                    currentVal <- v
                    changed.Trigger(v) // do first ?
                    this.OnPropertyChanged(nameof this.CurrentValue) 
        
        member val MinValBinding        = AutoPrecicionFloatBinding(this, nameof this.MinVal) 
        member val MaxValBinding        = AutoPrecicionFloatBinding(this, nameof this.MaxVal)
        member val CurrentValueBinding  = AutoPrecicionFloatBinding(this, nameof this.CurrentValue)

    
        /// reports the new value of the sliders cursor
        [<CLIEvent>]
        member _.Changed = changed.Publish
        
        /// slider minValue changed
        [<CLIEvent>]
        member _.MinChanged = minChanged.Publish
        
        /// slider maxValue changed
        [<CLIEvent>]
        member _.MaxChanged = maxChanged.Publish



    /// A View model for a slider control
    /// Includes a changed event if value changes
    type ManualPrecicionSliderViewModel (minVal:float, initalVal:float, maxVal:float, precicion:int) as this = 
        inherit ViewModelBase()
    
        let changed = new Event<float>()        
        let minChanged = new Event<float>()        
        let maxChanged = new Event<float>()        
    
        let mutable minVal = minVal
        let mutable maxVal = maxVal
        let mutable currentVal = initalVal 
        //let currentVal = ref initalVal 
    
        member this.MinVal
            with get()  = minVal
            and set(v) = 
                if v<>minVal then  
                    minVal <- v
                    this.OnPropertyChanged(nameof this.MinVal)
                    minChanged.Trigger(v) 
        
         member this.MaxVal
            with get()  = maxVal
            and set(v) =
                if v<>maxVal then 
                    maxVal <- v
                    this.OnPropertyChanged(nameof this.MaxVal)     
                    maxChanged.Trigger(v) 

        member this.CurrentValue
            with get()  = currentVal
            and set(v) =
                if v <> currentVal then 
                    currentVal <- v
                    this.OnPropertyChanged(nameof this.CurrentValue) 
                    changed.Trigger(v) 
        
        member val MinValBinding        = ManualPrecicionFloatBinding(this, nameof this.MinVal      , precicion    ) 
        member val MaxValBinding        = ManualPrecicionFloatBinding(this, nameof this.MaxVal      , precicion    )
        member val CurrentValueBinding  = ManualPrecicionFloatBinding(this, nameof this.CurrentValue, precicion    )

        
        member _.Precicion = precicion

        /// reports the new value of the sliders cursor
        [<CLIEvent>]
        member _.Changed = changed.Publish
        
        /// slider minValue changed
        [<CLIEvent>]
        member _.MinChanged = minChanged.Publish
        
        /// slider maxValue changed
        [<CLIEvent>]
        member _.MaxChanged = maxChanged.Publish

    
    
    type AutoPrecicionSliderPanel = {
        header      :TextBlock
        currentVal  :AutoPrecicionFloatTextBox
        minVal      :AutoPrecicionFloatTextBox
        slider      :SliderEx
        maxVal      :AutoPrecicionFloatTextBox
        panel       :DockPanel
        }
    
    type ManualPrecicionSliderPanel = {
        header      :TextBlock
        currentVal  :ManualPrecicionFloatTextBox
        minVal      :ManualPrecicionFloatTextBox
        slider      :SliderEx
        maxVal      :ManualPrecicionFloatTextBox
        panel       :DockPanel
        }
   

    let makeSliderPanelAutoPrecicion(label:string, sliderVM:AutoPrecicionSliderViewModel) :AutoPrecicionSliderPanel= 
        // make view
        let header  = TextBlock(               MinWidth = 200. ,  Margin = Thickness(3.),  TextAlignment=TextAlignment.Right )
        let curt    = AutoPrecicionFloatTextBox(MinWidth = 60.  ,  Margin = Thickness(6. , 3. , 6. , 3.)) 
        let mit     = AutoPrecicionFloatTextBox(MinWidth = 40.  ,  Margin = Thickness(3.),  TextAlignment=TextAlignment.Right ) 
        let slider  = SliderEx(                 MinWidth = 100. ,  Margin = Thickness(3.))    
        let mat     = AutoPrecicionFloatTextBox(MinWidth = 40.  ,  Margin = Thickness(3.)) 
   
        header.Text <- label + ":"
        mit.Background <- Brush.make(245,  245,  245) 
        mat.Background <- Brush.make(245,  245,  245)
    
        slider.Background <- Brush.make(255,  255,  255) 
        slider.Delay <- 100 
        

        let setChangeStep() = 
            slider.SmallChange <- (sliderVM.MaxVal - sliderVM.MinVal) / 40. // 40 steps in slider when pressing right or left arrow key
            slider.LargeChange <- (sliderVM.MaxVal - sliderVM.MinVal) / 20. // 20 steps in slider when klicking right or left of the current value cursor
        setChangeStep()
    
        //bind to view model
        mit.SetBinding   (TextBox.TextProperty  , sliderVM.MinValBinding      )    |> ignore 
        mat.SetBinding   (TextBox.TextProperty  , sliderVM.MaxValBinding      )    |> ignore 
        curt.SetBinding  (TextBox.TextProperty  , sliderVM.CurrentValueBinding)    |> ignore 
        slider.SetBinding(Slider.MinimumProperty, sliderVM.MinValBinding      )    |> ignore 
        slider.SetBinding(Slider.MaximumProperty, sliderVM.MaxValBinding      )    |> ignore 
        slider.SetBinding(Slider.ValueProperty  , sliderVM.CurrentValueBinding)    |> ignore  
        
        // explicit binding update ( for textboxes the  explicit  update is set in AutoPrecicionFloatTextBox class)
        slider.ValueChanged.Add (fun a -> 
            slider.GetBindingExpression(Slider.ValueProperty).UpdateSource() )//because the binding in text box is set to explicit update
            //slider.GetBindingExpression(Slider.ValueProperty).UpdateTarget() )

        sliderVM.MinChanged.Add ( fun _ -> setChangeStep())
        sliderVM.MaxChanged.Add ( fun _ -> setChangeStep())

        //Tooltip: //TODO make bindig to sliderVM.CurrentValueBinding to have same float formating
        slider.AutoToolTipPlacement <- Primitives.AutoToolTipPlacement.TopLeft
        let mutable prec = 0
        slider.AutoToolTipPrecision <- prec 
        slider.ValueChanged.Add ( fun a -> 
            let np = AutoPrecicionFormating.getPrecision a.NewValue
            if np<> prec then 
                prec <- np
                slider.AutoToolTipPrecision <- np)


        let d = new DockPanel()
        d.Background <- Brush.make(235,  235,  235) 
        d.Children
        |> add header
        |> add curt  
        |> add mit
        |> add mat
        |> add slider //added last so it will strech to fill remaining space
        |> ignore 
    
        DockPanel.SetDock(header,Dock.Left)
        DockPanel.SetDock(curt,Dock.Left)
        DockPanel.SetDock(mit,Dock.Left)
        DockPanel.SetDock(mat,Dock.Right)    
    
        {
        header=header
        currentVal = curt
        minVal= mit
        slider= slider
        maxVal= mat
        panel=d
        }

    
    let makeSliderPanelManualPrecicion(label:string, sliderVM:ManualPrecicionSliderViewModel) :ManualPrecicionSliderPanel=         
        
        
        // make view
        let header  = TextBlock(                    MinWidth = 200.  ,  Margin = Thickness(3.),  TextAlignment=TextAlignment.Right )
        let curt    = ManualPrecicionFloatTextBox(  MinWidth =  60.  ,  Margin = Thickness(6. , 3. , 6. , 3.)) 
        let mit     = ManualPrecicionFloatTextBox(  MinWidth =  40.  ,  Margin = Thickness(3.),  TextAlignment=TextAlignment.Right ) 
        let slider  = SliderEx(                     MinWidth = 100.  ,  Margin = Thickness(3.))    
        let mat     = ManualPrecicionFloatTextBox(  MinWidth =  40.  ,  Margin = Thickness(3.)) 
   
        header.Text <- label + ":"
        mit.Background <- Brush.make(245,  245,  245) 
        mat.Background <- Brush.make(245,  245,  245)
    
        slider.Background <- Brush.make(255,  255,  255) 
        slider.Delay <- 100  

        
        slider.SmallChange <- getTicks sliderVM.Precicion //  when pressing right or left arrow key
        slider.LargeChange <- getTicks sliderVM.Precicion // when klicking right or left of the current value cursor
        
    
        //bind to view model
        mit.SetBinding   (TextBox.TextProperty  , sliderVM.MinValBinding      )    |> ignore 
        mat.SetBinding   (TextBox.TextProperty  , sliderVM.MaxValBinding      )    |> ignore 
        curt.SetBinding  (TextBox.TextProperty  , sliderVM.CurrentValueBinding)    |> ignore 
        slider.SetBinding(Slider.MinimumProperty, sliderVM.MinValBinding      )    |> ignore 
        slider.SetBinding(Slider.MaximumProperty, sliderVM.MaxValBinding      )    |> ignore 
        slider.SetBinding(Slider.ValueProperty  , sliderVM.CurrentValueBinding)    |> ignore  
        
        // explicit binding update ( for textboxes the  explicit  update is set in AutoPrecicionFloatTextBox class)
        slider.ValueChanged.Add (fun a -> 
            slider.GetBindingExpression(Slider.ValueProperty).UpdateSource() )//because the binding in text box is set to explicit update
            //slider.GetBindingExpression(Slider.ValueProperty).UpdateTarget() )

 

        //Tooltip: //TODO make bindig to sliderVM.CurrentValueBinding to have same float formating
        slider.AutoToolTipPlacement <- Primitives.AutoToolTipPlacement.TopLeft        
        slider.AutoToolTipPrecision <- sliderVM.Precicion
        
        slider.IsSnapToTickEnabled <- true
        slider.TickFrequency <- getTicks sliderVM.Precicion 
        //slider.TickPlacement <- Primitives.TickPlacement.Both // draw ticks too?    

        let d = new DockPanel()
        d.Background <- Brush.make(235,  235,  235) 
        d.Children
        |> add header
        |> add curt  
        |> add mit
        |> add mat
        |> add slider //added last so it will strech to fill remaining space
        |> ignore 
    
        DockPanel.SetDock(header,Dock.Left)
        DockPanel.SetDock(curt,Dock.Left)
        DockPanel.SetDock(mit,Dock.Left)
        DockPanel.SetDock(mat,Dock.Right)    
        
     

        {
        header=header
        currentVal = curt
        minVal= mit
        slider= slider
        maxVal= mat
        panel=d
        }

(*
test:
Sync.doSync( fun ()  ->  
    
    printfn "KeepTextBoxDisplaySynchronizedWithTextProperty:%b" FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty 
    let win = PositionedWindow("GosTest") 
    let s1vm = SliderViewModel(-10.5 , 100.,  0.,  false)
    let s2vm = SliderViewModel(-9., 9.,  0., true)
    let s1 = makeSliderPanel("gos1"    , s1vm ) 
    let s2 = makeSliderPanel("gos zwei", s2vm) 
    let vst = StackPanel(Orientation=Orientation.Vertical) 
    vst.Children.Add s1 |> ignore 
    vst.Children.Add s2 |> ignore 
    win.Content <- vst
    
    //s1vm.Changed.Add(fun v -> printfn "%.16f" v) 
    //s2vm.Changed.Add(fun v -> printfn "%.16f" v) 
    
    win.Show() 
    ) 
*)
   