using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Bbr.Collections;

namespace Cerrio.Samples.SDC
{
    class ForceDirectedLayout
    {
        private readonly double m_width;
        private readonly double m_height;
        private double m_k;
        private int m_maxSteps = 200;
        private double m_c = 2;
        private double m_temperature;
        private IEnumerable<Item> m_items;
        private double m_minimumDistance;

        private Dictionary<Item, Vector> m_forces;
        private Random m_random = new Random();


        public ForceDirectedLayout(double width, double height)
        {
            m_width = width;
            m_height = height;
        }

        public void RandomLayout(IEnumerable<Item> items)
        {
            foreach (Item item in items)
            {
                item.X = m_random.NextDouble();
                item.Y = m_random.NextDouble();
            }
        }

        public void Layout(IEnumerable<Item> items)
        {
            m_items = items;
            m_forces = new Dictionary<Item, Vector>();

            double distance = Math.Sqrt(m_width * m_width + m_height * m_height);
            m_minimumDistance = distance / 100;
            m_k = (m_c * Math.Sqrt(distance / items.Count()));
            m_temperature = distance * .2;

            for (int i = 0; i < m_maxSteps; i++)
            {
                CalculateRepulsiveForces();
                CalculateAttractiveForce();
                DisplaceNodes();
                ReduceTemperature();
            }
        }


        private double RepulsiveForce(double distance)
        {
            return (m_k * m_k / distance);
        }

        private double AttractiveForce(double distance)
        {
            return (distance * distance / m_k);
        }

        private void ReduceTemperature()
        {
            m_temperature *= 0.991;
        }

        private void CalculateAttractiveForce()
        {
            foreach (Item start in m_items)
            {
                foreach (Item end in start.Dependencies)
                {
                    var delta = new Vector(start.X - end.X, start.Y - end.Y); ;
                    if (delta.Length > m_minimumDistance)
                    {
                        m_forces[start] -= delta / (float)delta.Length * AttractiveForce(delta.Length);
                        m_forces[end] += delta / (float)delta.Length * AttractiveForce(delta.Length);
                    }
                }
            }
        }

        private void CalculateRepulsiveForces()
        {
            foreach (Item item1 in m_items)
            {
                if (!m_forces.ContainsKey(item1))
                {
                    m_forces.Add(item1, new Vector(0,0));
                }

                foreach (Item item2 in m_items)
                {
                    if (item1 != item2)
                    {
                        if (item1.Distance(item2) < m_minimumDistance)
                        {
                            m_forces[item1] += new Vector(m_minimumDistance, m_minimumDistance);
                        }
                        else
                        {
                            Vector delta = new Vector(item1.X - item2.X, item1.Y - item2.Y);
                            m_forces[item1] += delta / (float)delta.Length * RepulsiveForce(delta.Length);
                        }
                    }
                }
            }
        }


        private void DisplaceNodes()
        {
            foreach (Item item in m_items)
            {
                Vector forceTemp = m_forces[item] / (float)m_forces[item].Length * (float)Math.Min(m_forces[item].Length, m_temperature);

                item.X += forceTemp.X;
                item.Y += forceTemp.Y;

                foreach (Item item2 in m_items)
                {
                    if (item != item2 && item.Distance(item2)<m_minimumDistance)
                    {
                        double move = m_minimumDistance - item.Distance(item2);
                        item.X += move / 2;
                        item.Y += move / 2;
                        item2.X -= move / 2;
                        item2.Y -= move / 2;
                    }
                }

                m_forces[item] = new Vector(0, 0);
            }

            BringPointsInBounds();
        }

        private void BringPointsInBounds()
        {
            foreach (Item item in m_items)
            {
                item.X = (float)Math.Max(0, Math.Min(m_width, item.X));
                item.Y = Math.Max(0, Math.Min(m_height, item.Y));
            }
        }
    }

    public class Item : IPosititonable
    {

        private string m_text;
        private List<Item> m_dependency = new List<Item>();

        public Item(string text)
        {
            Text = text;
        }

        public double X { get; set; }

        public double Y { get; set; }


        public string Text
        {
            get
            {
                return m_text;
            }
            set
            {
                m_text = value;
            }
        }

        public void AddDependency(Item item)
        {
            m_dependency.Add(item);
        }

        public double Distance(Item item)
        {
            return Math.Sqrt((X-item.X)*(X-item.X)
                +(Y-item.Y)*(Y-item.Y));
        }

        public IEnumerable<Item> Dependencies
        {
            get
            {
                return m_dependency;
            }
        }
    }

    public class BackedItem<T> : Item
    {
        public BackedItem(T item, string name)
            : base(name)
        {
            BackingItem = item;
        }

        public T BackingItem { get; private set; }
    }

    public class Vector
    {
        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }
        public double Y { get; set; }

        public double Length
        {
            get
            {
                return Math.Sqrt((X * X + Y * Y) / 2);
            }
        }

        public static Vector operator +(Vector v1,Vector v2)
        {
            return new Vector(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector operator -(Vector v1, Vector v2)
        {
            return new Vector(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Vector operator*(Vector v, double d)
        {
            return new Vector(v.X*d, v.Y*d);
        }

        public static Vector operator /(Vector v, double d)
        {
            return new Vector(v.X / d, v.Y / d);
        }
    }
}
