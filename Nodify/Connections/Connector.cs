using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Nodify
{
    /// <summary>
    /// Represents a connector control which starts a <see cref="PendingConnection"/> when being dragged and completes it when released.
    /// Has a <see cref="ElementConnector"/> that the <see cref="Anchor"/> is calculated from for the <see cref="PendingConnection"/>. Center of this control is used if missing.
    /// </summary>
    [TemplatePart(Name = ElementConnector, Type = typeof(FrameworkElement))]
    public class Connector : Control
    {
        // NodeInput.xaml, NodeOutput.xaml 에서
        // NodeInput.xaml
        //
        //                    <Control x:Name="PART_Connector"
        //                             Focusable="False"
        //                             Margin="0 0 5 0"
        //                             VerticalAlignment="Center"
        //                             Background="Transparent"
        //                             BorderBrush="{TemplateBinding BorderBrush}"
        //                             Template="{TemplateBinding ConnectorTemplate}" />
        // NodeOutput.xaml
        // 
        //                    <Control x:Name="PART_Connector"
        //                             Focusable="False"
        //                             Margin="5 0 0 0"
        //                             VerticalAlignment="Center"
        //                             Background="Transparent"
        //                             BorderBrush="{TemplateBinding BorderBrush}"
        //                             Template="{TemplateBinding ConnectorTemplate}" />
        // 이렇게 정의되어 있음

        protected const string ElementConnector = "PART_Connector";
       
        #region Routed Events

        public static readonly RoutedEvent PendingConnectionStartedEvent = EventManager.RegisterRoutedEvent(nameof(PendingConnectionStarted), RoutingStrategy.Bubble, typeof(PendingConnectionEventHandler), typeof(Connector));
        public static readonly RoutedEvent PendingConnectionCompletedEvent = EventManager.RegisterRoutedEvent(nameof(PendingConnectionCompleted), RoutingStrategy.Bubble, typeof(PendingConnectionEventHandler), typeof(Connector));
        public static readonly RoutedEvent PendingConnectionDragEvent = EventManager.RegisterRoutedEvent(nameof(PendingConnectionDrag), RoutingStrategy.Bubble, typeof(PendingConnectionEventHandler), typeof(Connector));
        public static readonly RoutedEvent DisconnectEvent = EventManager.RegisterRoutedEvent(nameof(Disconnect), RoutingStrategy.Bubble, typeof(ConnectorEventHandler), typeof(Connector));

        /// <summary>
        /// Occurs when the <see cref="Connector"/> is clicked.
        /// </summary>
        public event PendingConnectionEventHandler PendingConnectionStarted
        {
            add => AddHandler(PendingConnectionStartedEvent, value);
            remove => RemoveHandler(PendingConnectionStartedEvent, value);
        }

        /// <summary>
        /// Occurs when the <see cref="Connector"/> loses mouse capture.
        /// </summary>
        public event PendingConnectionEventHandler PendingConnectionCompleted
        {
            add => AddHandler(PendingConnectionCompletedEvent, value);
            remove => RemoveHandler(PendingConnectionCompletedEvent, value);
        }

        /// <summary>
        /// Occurs when the mouse is changing position and the <see cref="Connector"/> has mouse capture.
        /// </summary>
        public event PendingConnectionEventHandler PendingConnectionDrag
        {
            add => AddHandler(PendingConnectionDragEvent, value);
            remove => RemoveHandler(PendingConnectionDragEvent, value);
        }

        /// <summary>
        /// Occurs when the <see cref="ModifierKeys.Alt"/> key is held and the <see cref="Connector"/> is clicked.
        /// </summary>
        public event ConnectorEventHandler Disconnect
        {
            add => AddHandler(DisconnectEvent, value);
            remove => RemoveHandler(DisconnectEvent, value);
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty AnchorProperty = DependencyProperty.Register(nameof(Anchor), typeof(Point), typeof(Connector), new FrameworkPropertyMetadata(BoxValue.Point));
        public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register(nameof(IsConnected), typeof(bool), typeof(Connector), new FrameworkPropertyMetadata(BoxValue.False, OnIsConnectedChanged));
        public static readonly DependencyProperty DisconnectCommandProperty = DependencyProperty.Register(nameof(DisconnectCommand), typeof(ICommand), typeof(Connector));
        private static readonly DependencyPropertyKey _isPendingConnectionPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsPendingConnection), typeof(bool), typeof(Connector), new FrameworkPropertyMetadata(BoxValue.False));
        public static readonly DependencyProperty IsPendingConnectionProperty = _isPendingConnectionPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the location where <see cref="Connection"/>s can be attached to. 
        /// Bind with <see cref="System.Windows.Data.BindingMode.OneWayToSource"/>
        /// </summary>
        public Point Anchor
        {
            get => (Point)GetValue(AnchorProperty);
            set => SetValue(AnchorProperty, value);
        }

        /// <summary>
        /// If this is set to false, the <see cref="Disconnect"/> event will not be invoked and the connector will stop updating its <see cref="Anchor"/> when moved, resized etc.
        /// </summary>
        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether a <see cref="PendingConnection"/> is in progress for this <see cref="Connector"/>.
        /// </summary>
        public bool IsPendingConnection
        {
            get => (bool)GetValue(IsPendingConnectionProperty);
            protected set => SetValue(_isPendingConnectionPropertyKey, value);
        }

        /// <summary>
        /// Invoked if the <see cref="Disconnect"/> event is not handled.
        /// Parameter is the <see cref="FrameworkElement.DataContext"/> of this control.
        /// </summary>
        public ICommand DisconnectCommand
        {
            get => (ICommand)GetValue(DisconnectCommandProperty);
            set => SetValue(DisconnectCommandProperty, value);
        }

        #endregion

        static Connector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Connector), new FrameworkPropertyMetadata(typeof(Connector)));
            FocusableProperty.OverrideMetadata(typeof(Connector), new FrameworkPropertyMetadata(BoxValue.True));
        }

        #region Fields

        /// <summary>
        /// Gets the <see cref="FrameworkElement"/> used to calculate the <see cref="Anchor"/>.
        /// </summary>
        protected FrameworkElement? Thumb { get; private set; }

        /// <summary>
        /// Gets the <see cref="ItemContainer"/> that contains this <see cref="Connector"/>.
        /// </summary>
        protected ItemContainer? Container { get; private set; }

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="Container"/>.
        /// </summary>
        /// C# 6.0 이상에서 private set; 즉 읽기전용 프로퍼티에서 private set 의 생략이 가능해진다.
        protected NodifyEditor? Editor { get; private set; }

        /// <summary>
        /// Gets or sets the safe zone outside the <see cref="NodifyEditor.Viewport"/> that will not trigger optimizations.
        /// </summary>
        public static double OptimizeSafeZone = 1000d;

        /// <summary>
        /// Gets or sets the minimum selected items needed to trigger optimizations when outside of the <see cref="OptimizeSafeZone"/>.
        /// </summary>
        public static uint OptimizeMinimumSelectedItems = 100;

        /// <summary>
        /// Gets or sets if <see cref="Connector"/>s should enable optimizations based on <see cref="OptimizeSafeZone"/> and <see cref="OptimizeMinimumSelectedItems"/>.
        /// </summary>
        public static bool EnableOptimizations = true;

        /// <summary>
        /// Gets or sets whether cancelling a pending connection is allowed.
        /// </summary>
        public static bool AllowPendingConnectionCancellation { get; set; } = true;

        private Point _lastUpdatedContainerPosition;
        private Point _thumbCenter;
        private bool _isHooked;

        #endregion


        /*
            public class Connector : Control : FrameworkElement 이렇게 파생을 받았다.
            FrameworkElement 파생 된 클래스는 OnApplyTemplate 클래스 처리기를 사용 하 여이 
            메서드가 명시적으로 호출 된 사례 또는 레이아웃 시스템에 대 한 알림 메시지를 받을 수 있습니다. 
            OnApplyTemplate 는 템플릿이 완전히 생성 되 고 논리 트리에 연결 된 후에 호출 됩니다. 
        
            약간 init() 느낌의 함수임.
         */
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Thumb = Template.FindName(ElementConnector, this) as FrameworkElement ?? this;

            
            
            /*
                 this.GetParentOfType 에서 this 는 NodeInput 이다. 이클래스는 Connector 인데 물론 NodeInput 은 Connector 로 상속을 받았다.
                 왜 this 가 NodeInput 인지 찾아야 한다.
                 사실 Node 가 생성 되는 시점도 아직 파악하고 있지 못하다. 이부분을 찾아야 한다.
             */
            Container = this.GetParentOfType<ItemContainer>();  // NodeInput 의 부모 ItemContainer 를 찾는다. 실제로 NodeInput, NodeOutput 은 Connector 에서 상속받았다.

            if (Container?.Editor != null)
                Editor = Container?.Editor;
            else {
               Editor = this.GetParentOfType<NodifyEditor>();
            };

            //Editor = Container?.Editor ?? this.GetParentOfType<NodifyEditor>();  

            Loaded += OnConnectorLoaded;
            Unloaded += OnConnectorUnloaded;
        }

        #region Update connector

        // Toggle events that could be used to update the Anchor
        private void TrySetAnchorUpdateEvents(bool value)
        {
            if (Container != null && Editor != null)
            {
                // If events are not already hooked and we are asked to subscribe
                if (value && !_isHooked)
                {
                    Container.PreviewLocationChanged += UpdateAnchorOptimized;
                    Container.LocationChanged += OnLocationChanged;
                    Editor.ViewportUpdated += OnViewportUpdated;
                    _isHooked = true;
                }
                // If events are already hooked and we are asked to unsubscribe
                else if (_isHooked && !value)
                {
                    Container.PreviewLocationChanged -= UpdateAnchorOptimized;
                    Container.LocationChanged -= OnLocationChanged;
                    Editor.ViewportUpdated -= OnViewportUpdated;
                    _isHooked = false;
                }
            }
        }

        private void OnConnectorLoaded(object sender, RoutedEventArgs? e)
            => TrySetAnchorUpdateEvents(true);

        private void OnConnectorUnloaded(object sender, RoutedEventArgs e)
            => TrySetAnchorUpdateEvents(false);

        private static void OnIsConnectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var con = (Connector)d;

            if ((bool)e.NewValue)
            {
                con.UpdateAnchor();
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            // Subscribe to events if not already subscribed 
            // Useful for advanced connectors that start collapsed because the loaded event is not called
            var newSize = sizeInfo.NewSize;
            if (newSize.Width > 0 || newSize.Height > 0)
            {
                TrySetAnchorUpdateEvents(true);

                UpdateAnchorOptimized(Container!.Location);
            }
        }

        private void OnLocationChanged(object sender, RoutedEventArgs e)
            => UpdateAnchorOptimized(Container!.Location);

        private void OnViewportUpdated(object sender, RoutedEventArgs args)
        {
            if (Container != null && !Container.IsPreviewingLocation && _lastUpdatedContainerPosition != Container.Location)
            {
                UpdateAnchorOptimized(Container.Location);
            }
        }

        /// <summary>
        /// Updates the <see cref="Anchor"/> and applies optimizations if needed based on <see cref="EnableOptimizations"/> flag
        /// </summary>
        /// <param name="location"></param>
        protected void UpdateAnchorOptimized(Point location)
        {
            // Update only connectors that are connected
            if (Editor != null && IsConnected)
            {
                bool shouldOptimize = EnableOptimizations && Editor.SelectedItems?.Count > OptimizeMinimumSelectedItems;

                if (shouldOptimize)
                {
                    UpdateAnchorBasedOnLocation(Editor, location);
                }
                else
                {
                    UpdateAnchor(location);
                }
            }
        }

        private void UpdateAnchorBasedOnLocation(NodifyEditor editor, Point location)
        {
            var viewport = editor.Viewport;
            var offset = OptimizeSafeZone / editor.Scale;

            var area = Rect.Inflate(viewport, offset, offset);

            // Update only the connectors that are in the viewport or will be in the viewport
            if (area.Contains(location))
            {
                UpdateAnchor(location);
            }
        }

        /// <summary>
        /// Updates the <see cref="Anchor"/> relative to a location. (usually <see cref="Container"/>'s location)
        /// </summary>
        /// <param name="location">The relative location</param>
        protected void UpdateAnchor(Point location)
        {
            _lastUpdatedContainerPosition = location;

            if (Thumb != null && Container != null)
            {
                var thumbSize = (Vector)Thumb.RenderSize;
                var containerMargin = (Vector)Container.RenderSize - (Vector)Container.DesiredSize;
                Point relativeLocation = Thumb.TranslatePoint((Point)(thumbSize / 2 - containerMargin / 2), Container);
                Anchor = new Point(location.X + relativeLocation.X, location.Y + relativeLocation.Y);
            }
        }

        /// <summary>
        /// Updates the <see cref="Anchor"/> based on <see cref="Container"/>'s location.
        /// </summary>
        public void UpdateAnchor()
        {
            if (Container != null)
            {
                UpdateAnchor(Container.Location);
            }
        }

        #endregion

        #region Event Handlers

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (IsPendingConnection)
            {
                OnConnectorDragCompleted(cancel: AllowPendingConnectionCancellation);
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            // Cancel pending connection
            if (AllowPendingConnectionCancellation && IsMouseCaptured && IsPendingConnection)
            {
                OnConnectorDragCompleted(cancel: true);
                ReleaseMouseCapture();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Focus();
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                OnDisconnect();
            }
            else
            {
                UpdateAnchor();
                OnConnectorDragStarted();

                CaptureMouse();
            }

            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            Focus();

            if (IsMouseCaptured)
            {
                OnConnectorDragCompleted();
                ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            {
                var offset = e.GetPosition(Thumb) - _thumbCenter;
                OnConnectorDrag(offset);

                e.Handled = true;
            }
        }

        protected virtual void OnConnectorDrag(Vector offset)
        {
            var args = new PendingConnectionEventArgs(DataContext)
            {
                RoutedEvent = PendingConnectionDragEvent,
                OffsetX = offset.X,
                OffsetY = offset.Y,
                Anchor = Anchor,
                Source = this
            };

            RaiseEvent(args);
        }

        protected virtual void OnConnectorDragStarted()
        {
            if (Thumb != null)
            {
                _thumbCenter = new Point(Thumb.ActualWidth / 2, Thumb.ActualHeight / 2);
            }

            var args = new PendingConnectionEventArgs(DataContext) // PendingConnectionEventArgs.SourceConnector 에 DataContext 를 연결 시킴.
            {
                // Anchor 는 PendingConnectionEventArgs
                // RoutedEvent, Source 는 RoutedEventArgs
                // Source 이벤트를 발생시킨 개체에 대한 참조를 가져오거나 설정합니다.
                // RoutedEvent 이 RoutedEventArgs 인스턴스와 연결된 RoutedEvent를 가져오거나 설정합니다.

                RoutedEvent = PendingConnectionStartedEvent,
                // Anchor 는 Connector 에서 받아온 Anchor 임. 근데 Anchor 는 뭐지???
                Anchor = Anchor,
                Source = this
            };

            RaiseEvent(args);   // 여기서 PendingConnectionStartedEvent 이 이벤트 발생시킴. RaiseEvent(RoutedEventArgs)
            IsPendingConnection = true;
        }

        /*
         * 이 함수에서 PendingConnectionCompletedEvent 에 대한 이벤트를 발생시켜 준다.
         */
        protected virtual void OnConnectorDragCompleted(bool cancel = false)
        {
            FrameworkElement? elem = null;
            if (Editor != null)
            {
                elem = PendingConnection.GetPotentialConnector(Editor, PendingConnection.GetAllowOnlyConnectorsAttached(Editor));

            }

            var target = elem?.DataContext;

            /*  var args = new PendingConnectionEventArgs(DataContext)
              {
                  TargetConnector = target,
                  RoutedEvent = PendingConnectionCompletedEvent,
                  Anchor = Anchor,
                  Source = this,
                  Canceled = cancel //  Gets or sets a value that indicates whether this <see cref="PendingConnection"/> was cancelled.
              };*/

            var args1 = new PendingConnectionEventArgs(DataContext);                    // 여기서 SourceConnector 세팅

            PendingConnectionEventArgs args = args1 as PendingConnectionEventArgs;
           
            args.TargetConnector = target;                                              // 여기서 TargetConnector 세팅
            args.RoutedEvent = PendingConnectionCompletedEvent;
            args.Anchor = Anchor;
            args.Source = this;
            args.Canceled = cancel;

            IsPendingConnection = false;
            RaiseEvent(args);
        }

        protected virtual void OnDisconnect()
        {
            if (IsConnected)
            {
                var connector = DataContext;
                var args = new ConnectorEventArgs(connector)
                {
                    RoutedEvent = DisconnectEvent,
                    Anchor = Anchor,
                    Source = this
                };

                RaiseEvent(args);

                // Raise DisconnectCommand if event is Disconnect not handled
                if (!args.Handled && (DisconnectCommand?.CanExecute(connector) ?? false))
                {
                    DisconnectCommand.Execute(connector);
                }
            }
        }

        #endregion
    }
}
