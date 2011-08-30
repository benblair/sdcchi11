using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Bbr.Extensions;
using QuickGraph;
using GraphSharp;
using Bbr.Collections;
using GraphSharp.Algorithms.Layout;

namespace Cerrio.Samples.SDC
{
    class GraphSharp<TGraphItem> 
        where TGraphItem:class,IPosititonable
    {
        //CircularLayoutAlgorithm<TGraphItem, GraphEdge, BidirectionalGraph<TGraphItem, GraphEdge>> m_algo;
        BidirectionalGraph<TGraphItem,GraphEdge> m_graph;
        StandardLayoutAlgorithmFactory<TGraphItem, GraphEdge, BidirectionalGraph<TGraphItem, GraphEdge>> m_factory;

        public GraphSharp(IEnumerable<TGraphItem> items,IEnumerable<Pair<TGraphItem,TGraphItem>> edges)
        {
            m_graph = GraphHelper.CreateGraph(items,edges,p=>new GraphEdge(p.First,p.Second));
            m_factory = new StandardLayoutAlgorithmFactory<TGraphItem, GraphEdge, BidirectionalGraph<TGraphItem, GraphEdge>>();
        }

        public IEnumerable<TGraphItem> LayoutLinLog()
        {
            return Layout("LinLog");
        }

        public IEnumerable<TGraphItem> LayoutCircular()
        {
            return Layout("Circular");
        }

        public IEnumerable<TGraphItem> Layout(string type)
        {
            var algo = m_factory.CreateAlgorithm(type, new MyLayoutContext(m_graph), null);
            
            if(!algo.VertexPositions.Any())
            {
                return new List<TGraphItem>();
            }
            
            algo.Compute();

            double maxX = algo.VertexPositions.Values.Max(m => m.X);
            double minX = algo.VertexPositions.Values.Min(m => m.X);
            double maxY = algo.VertexPositions.Values.Max(m => m.Y);
            double minY = algo.VertexPositions.Values.Min(m => m.Y);

            foreach (TGraphItem item in algo.VertexPositions.Keys)
            {
                Point pp = algo.VertexPositions[item];
                item.X = (pp.X - minX) / (maxX - minX);
                item.Y = (pp.Y - minY) / (maxY - minY);
            }

            return algo.VertexPositions.Keys;
        }

        public IEnumerable<string> GetAlgoTypes()
        {
            return m_factory.AlgorithmTypes;
        }

        class GraphEdge : IEdge<TGraphItem>
        {
            public GraphEdge(TGraphItem source, TGraphItem target)
            {
                Source = source;
                Target = target;
            }

            public TGraphItem Source { get; private set; }
            public TGraphItem Target { get; private set; }
        }

        class MyLayoutContext : ILayoutContext<TGraphItem, GraphEdge, BidirectionalGraph<TGraphItem, GraphEdge>>
        {
            public MyLayoutContext(BidirectionalGraph<TGraphItem, GraphEdge> graph)
            {
                Graph = graph;
                Mode = LayoutMode.Simple;
            }


            public BidirectionalGraph<TGraphItem, GraphEdge> Graph { get; private set; }
            public LayoutMode Mode { get; private set; }

            public IDictionary<TGraphItem, Point> Positions
            {
                get
                {
                    Dictionary<TGraphItem, Point> positions = new Dictionary<TGraphItem, Point>();
                    Graph.Vertices.ForEach(v => positions.Add(v, new Point(v.X,v.Y)));
                    return positions;
                }
            }

            public IDictionary<TGraphItem, Size> Sizes
            {
                get
                {
                    Dictionary<TGraphItem, Size> sizes = new Dictionary<TGraphItem, Size>();
                    Graph.Vertices.ForEach(v => sizes.Add(v, new Size(50, 50)));
                    return sizes;
                }
            }
        }
    }

    
}
