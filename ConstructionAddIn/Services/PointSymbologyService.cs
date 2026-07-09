//using ArcGIS.Core.CIM;
//using ArcGIS.Core.Geometry;
//using ArcGIS.Desktop.Framework.Threading.Tasks;
//using ArcGIS.Desktop.Mapping;


//namespace ConstructionAddIn.Services
//{
//    public static class PointSymbologyService
//    {
//        public static async void ApplyValveRegulatorSymbol(FeatureLayer layer)
//        {
//            if (layer == null)
//                return;

//            await QueuedTask.Run(() =>
//            {
//                var pointSymbol = SymbolFactory.Instance.ConstructPointSymbol(
//                    ColorFactory.Instance.BlackRGB,
//                    18,
//                    SimpleMarkerStyle.Diamond);

//                var renderer = new CIMSimpleRenderer
//                {
//                    Symbol = pointSymbol.MakeSymbolReference(),
//                    VisualVariables = new CIMVisualVariable[]
//                    {
//                        new CIMRotationVisualVariable
//                        {
//                            VisualVariableInfoZ = new CIMVisualVariableInfo
//                            {
//                                ValueExpressionInfo = new CIMExpressionInfo
//                                {
//                                    Expression = "$feature.ANGLE",
//                                    ReturnType = ExpressionReturnType.Default
//                                }
//                            },
//                            RotationTypeZ = SymbolRotationType.Arithmetic
//                        }
//                    }
//                };

//                layer.SetRenderer(renderer);
//            });
//        }

//        /// <summary>
//        /// Applies a temporary Valve-Pecat symbol to the target feature layer.
//        /// The symbol rotates using the ANGLE field.
//        /// </summary>
//        public static async void ApplyValvePecatSymbol(FeatureLayer layer)
//        {
//            if (layer == null)
//                return;

//            await QueuedTask.Run(() =>
//            {
//                var pointSymbol = SymbolFactory.Instance.ConstructPointSymbol(
//                    ColorFactory.Instance.BlackRGB,
//                    18,
//                    SimpleMarkerStyle.Diamond);

//                var renderer = new CIMSimpleRenderer
//                {
//                    Symbol = pointSymbol.MakeSymbolReference(),
//                    VisualVariables = new CIMVisualVariable[]
//                    {
//                new CIMRotationVisualVariable
//                {
//                    VisualVariableInfoZ = new CIMVisualVariableInfo
//                    {
//                        ValueExpressionInfo = new CIMExpressionInfo
//                        {
//                            Expression = "$feature.ANGLE",
//                            ReturnType = ExpressionReturnType.Default
//                        }
//                    },
//                    RotationTypeZ = SymbolRotationType.Arithmetic
//                }
//                    }
//                };

//                layer.SetRenderer(renderer);
//            });
//        }

//        /// <summary>
//        /// Applies a temporary Valve-Pecat-(1)Vent symbol.
//        /// The symbol rotates using the ANGLE field.
//        /// </summary>
//        public static async void ApplyValve1VentPecatSymbol(FeatureLayer layer)
//        {
//            if (layer == null)
//                return;

//            await QueuedTask.Run(() =>
//            {
//                var blackFill = SymbolFactory.Instance.ConstructPolygonSymbol(
//                    ColorFactory.Instance.BlackRGB,
//                    SimpleFillStyle.Solid);

//                var blackLine = SymbolFactory.Instance.ConstructLineSymbol(
//                    ColorFactory.Instance.BlackRGB,
//                    1.6);

//                var marker = new CIMVectorMarker
//                {
//                    Enable = true,
//                    Size = 24,
//                    MarkerGraphics = new CIMMarkerGraphic[]
//                    {
//                // Left double bar
//                new() {
//                    Geometry = PolygonBuilderEx.CreatePolygon(new[]
//                    {
//                        MapPointBuilderEx.CreateMapPoint(-12.5, -4),
//                        MapPointBuilderEx.CreateMapPoint(-11.5, -4),
//                        MapPointBuilderEx.CreateMapPoint(-11.5, 4),
//                        MapPointBuilderEx.CreateMapPoint(-12.5, 4)
//                    }),
//                    Symbol = blackFill
//                },

//                new() {
//                    Geometry = PolygonBuilderEx.CreatePolygon(
//                    [
//                        MapPointBuilderEx.CreateMapPoint(-10.5, -4),
//                        MapPointBuilderEx.CreateMapPoint(-9.5, -4),
//                        MapPointBuilderEx.CreateMapPoint(-9.5, 4),
//                        MapPointBuilderEx.CreateMapPoint(-10.5, 4)
//                    ]),
//                    Symbol = blackFill
//                },

//                // Right double bar
//                new() {
//                    Geometry = PolygonBuilderEx.CreatePolygon(
//                    [
//                        MapPointBuilderEx.CreateMapPoint(9.5, -4),
//                        MapPointBuilderEx.CreateMapPoint(10.5, -4),
//                        MapPointBuilderEx.CreateMapPoint(10.5, 4),
//                        MapPointBuilderEx.CreateMapPoint(9.5, 4)
//                    ]),
//                    Symbol = blackFill
//                },

//                new() {
//                    Geometry = PolygonBuilderEx.CreatePolygon(
//                    [
//                        MapPointBuilderEx.CreateMapPoint(11.5, -4),
//                        MapPointBuilderEx.CreateMapPoint(12.5, -4),
//                        MapPointBuilderEx.CreateMapPoint(12.5, 4),
//                        MapPointBuilderEx.CreateMapPoint(11.5, 4)
//                    ]),
//                    Symbol = blackFill
//                },

//                // Valve left triangle
//                new() {
//                    Geometry = PolygonBuilderEx.CreatePolygon(
//                    [
//                        MapPointBuilderEx.CreateMapPoint(-6, -5),
//                        MapPointBuilderEx.CreateMapPoint(0, 0),
//                        MapPointBuilderEx.CreateMapPoint(-6, 5)
//                    ]),
//                    Symbol = blackFill
//                },

//                // Valve right triangle
//                new() {
//                    Geometry = PolygonBuilderEx.CreatePolygon(
//                    [
//                        MapPointBuilderEx.CreateMapPoint(6, -5),
//                        MapPointBuilderEx.CreateMapPoint(0, 0),
//                        MapPointBuilderEx.CreateMapPoint(6, 5)
//                    ]),
//                    Symbol = blackFill
//                },

//                // Vent stem
//                new() {
//                    Geometry = PolylineBuilderEx.CreatePolyline(
//                    [
//                        MapPointBuilderEx.CreateMapPoint(-7, 4),
//                        MapPointBuilderEx.CreateMapPoint(-7, 10)
//                    ]),
//                    Symbol = blackLine
//                },

//                // Vent lower triangle
//                new() {
//                    Geometry = PolygonBuilderEx.CreatePolygon(
//                    [
//                        MapPointBuilderEx.CreateMapPoint(-10, 10),
//                        MapPointBuilderEx.CreateMapPoint(-4, 10),
//                        MapPointBuilderEx.CreateMapPoint(-7, 6)
//                    ]),
//                    Symbol = blackFill
//                },

//                // Vent upper triangle
//                new() {
//                    Geometry = PolygonBuilderEx.CreatePolygon(
//                    [
//                        MapPointBuilderEx.CreateMapPoint(-10, 11),
//                        MapPointBuilderEx.CreateMapPoint(-4, 11),
//                        MapPointBuilderEx.CreateMapPoint(-7, 15)
//                    ]),
//                    Symbol = blackFill
//                }
//                    }
//                };

//                var pointSymbol = new CIMPointSymbol
//                {
//                    SymbolLayers =
//                    [
//                marker
//                    ]
//                };

//                var renderer = new CIMSimpleRenderer
//                {
//                    Symbol = pointSymbol.MakeSymbolReference(),
//                    VisualVariables =
//                    [
//                new CIMRotationVisualVariable
//                {
//                    VisualVariableInfoZ = new CIMVisualVariableInfo
//                    {
//                        ValueExpressionInfo = new CIMExpressionInfo
//                        {
//                            Expression = "$feature.ANGLE",
//                            ReturnType = ExpressionReturnType.Default
//                        }
//                    },
//                    RotationTypeZ = SymbolRotationType.Arithmetic
//                }
//                    ]
//                };

//                layer.SetRenderer(renderer);
//            });
//        }


//        /// <summary>
//        /// Applies a temporary Ball-Valve-(1)Vent symbol.
//        /// The symbol rotates using the ANGLE field.
//        /// </summary>
//        public static async void ApplyBallValve1VentSymbol(FeatureLayer layer)
//        {
//            if (layer == null)
//                return;

//            await QueuedTask.Run(() =>
//            {
//                var pointSymbol = SymbolFactory.Instance.ConstructPointSymbol(
//                    ColorFactory.Instance.BlackRGB,
//                    18,
//                    SimpleMarkerStyle.Diamond);

//                var renderer = new CIMSimpleRenderer
//                {
//                    Symbol = pointSymbol.MakeSymbolReference(),
//                    VisualVariables = new CIMVisualVariable[]
//                    {
//                new CIMRotationVisualVariable
//                {
//                    VisualVariableInfoZ = new CIMVisualVariableInfo
//                    {
//                        ValueExpressionInfo = new CIMExpressionInfo
//                        {
//                            Expression = "$feature.ANGLE",
//                            ReturnType = ExpressionReturnType.Default
//                        }
//                    },
//                    RotationTypeZ = SymbolRotationType.Arithmetic
//                }
//                    }
//                };

//                layer.SetRenderer(renderer);
//            });
//        }

//        /// <summary>
//        /// Applies a temporary Valve-Pecat-(2)Vent symbol.
//        /// The symbol rotates using the ANGLE field.
//        /// </summary>
//        public static async void ApplyValve2VentsPecatSymbol(FeatureLayer layer)
//        {
//            if (layer == null)
//                return;

//            await QueuedTask.Run(() =>
//            {
//                var pointSymbol = SymbolFactory.Instance.ConstructPointSymbol(
//                    ColorFactory.Instance.BlackRGB,
//                    18,
//                    SimpleMarkerStyle.Diamond);

//                var renderer = new CIMSimpleRenderer
//                {
//                    Symbol = pointSymbol.MakeSymbolReference(),
//                    VisualVariables = new CIMVisualVariable[]
//                    {
//                new CIMRotationVisualVariable
//                {
//                    VisualVariableInfoZ = new CIMVisualVariableInfo
//                    {
//                        ValueExpressionInfo = new CIMExpressionInfo
//                        {
//                            Expression = "$feature.ANGLE",
//                            ReturnType = ExpressionReturnType.Default
//                        }
//                    },
//                    RotationTypeZ = SymbolRotationType.Arithmetic
//                }
//                    }
//                };

//                layer.SetRenderer(renderer);
//            });
//        }

//        public static async void ApplyReducerSymbol(FeatureLayer layer)
//        {
//            if (layer == null)
//                return;

//            await QueuedTask.Run(() =>
//            {
//                var pointSymbol = SymbolFactory.Instance.ConstructPointSymbol(
//                    ColorFactory.Instance.BlackRGB,
//                    18,
//                    SimpleMarkerStyle.Triangle);

//                var renderer = new CIMSimpleRenderer
//                {
//                    Symbol = pointSymbol.MakeSymbolReference(),
//                    VisualVariables = new CIMVisualVariable[]
//                    {
//                new CIMRotationVisualVariable
//                {
//                    VisualVariableInfoZ = new CIMVisualVariableInfo
//                    {
//                        ValueExpressionInfo = new CIMExpressionInfo
//                        {
//                            Expression = "$feature.ANGLE",
//                            ReturnType = ExpressionReturnType.Default
//                        }
//                    },
//                    RotationTypeZ = SymbolRotationType.Arithmetic
//                }
//                    }
//                };

//                layer.SetRenderer(renderer);
//            });
//        }
//    }
//}