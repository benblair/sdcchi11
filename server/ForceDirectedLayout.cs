using System;
using System.Collections.Generic;
using System.Linq;

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
        private IEnumerable<IPosititonable> m_items;
        private double m_minimumDistance;

        private Dictionary<IPosititonable, Vector> m_forces;
        private Random m_random = new Random();


        public ForceDirectedLayout(double width, double height)
        {
            m_width = width;
            m_height = height;
        }

        public void RandomLayout(IEnumerable<IPosititonable> items)
        {
            foreach (IPosititonable item in items)
            {
                item.X = m_random.NextDouble();
                item.Y = m_random.NextDouble();
            }
        }

        public void Layout(IEnumerable<IPosititonable> items)
        {
            if(items.Any(i=>double.IsNaN(i.X)||double.IsNaN(i.Y)))
            {
                throw new Exception("All input values must have a valid X,Y coordinate");
            }

            m_items = items;
            m_forces = new Dictionary<IPosititonable, Vector>();

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

            int itterations = 5;
            bool workDone;
            do
            {
                itterations--;
                workDone = SpaceNodes();
                BringPointsInBounds();
            } while (workDone && itterations >0);

            if (items.Any(it => double.IsNaN(it.X) || double.IsNaN(it.Y)))
            {
                throw new Exception("layout screwed something up");
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
            foreach (IPosititonable start in m_items)
            {
                foreach (IPosititonable end in start.Dependencies)
                {
                    var delta = new Vector(start.X - end.X, start.Y - end.Y);
                    if (delta.Length !=0)
                    {
                        m_forces[start] -= delta / (float)delta.Length * AttractiveForce(delta.Length);
                        m_forces[end] += delta / (float)delta.Length * AttractiveForce(delta.Length);
                    }
                }
            }
        }

        private void CalculateRepulsiveForces()
        {
            foreach (IPosititonable item1 in m_items)
            {
                if (!m_forces.ContainsKey(item1))
                {
                    m_forces.Add(item1, new Vector(0,0));
                }

                foreach (IPosititonable item2 in m_items)
                {
                    if (item1 != item2)
                    {
                        Vector delta = new Vector(item1.X - item2.X, item1.Y - item2.Y);

                        if(delta.Length==0)
                        {
                            delta = Vector.RandomVector();
                        }

                        m_forces[item1] += delta / (float)delta.Length * RepulsiveForce(delta.Length);
                    }
                }
            }
        }


        private void DisplaceNodes()
        {
            foreach (IPosititonable item in m_items)
            {
                Vector forceTemp = m_forces[item] / (float)m_forces[item].Length * (float)Math.Min(m_forces[item].Length, m_temperature);

                item.X += forceTemp.X;
                item.Y += forceTemp.Y;

                m_forces[item] = new Vector(0, 0);
            }

            SpaceNodes();
            BringPointsInBounds();
        }

        private bool SpaceNodes()
        {
            bool found = false;
            foreach (IPosititonable item in m_items)
            {
                foreach (IPosititonable item2 in m_items)
                {
                    Vector delta = new Vector(item.X - item2.X, item.Y - item2.Y);
                    
                    if (item != item2 && delta.Length < m_minimumDistance)
                    {
                        Vector move;
                        if(delta.Length==0)
                        {
                            move = Vector.RandomVector();
                            move *= (m_minimumDistance/move.Length)/1.9;
                        }
                        else
                        {
                            move = delta * ((m_minimumDistance-delta.Length) / delta.Length) / 1.9;
                        }

                        item.X += move.X;
                        item.Y += move.Y;
                        item2.X -= move.X;
                        item2.Y -= move.Y;
                        found = true;
                    }
                }
            }

            return found;
        }

        private void BringPointsInBounds()
        {
            foreach (IPosititonable item in m_items)
            {
                item.X = (float)Math.Max(0, Math.Min(m_width, item.X));
                item.Y = Math.Max(0, Math.Min(m_height, item.Y));
            }
        }
    }

    public class Vector
    {
        public Vector(double x, double y)
        {
            X = x;
            Y = y;
            Length=Math.Sqrt(x * x + y * y);
        }

        public double X { get; private set; }
        public double Y { get; private set; }

        public double Length { get; private set; }

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
            if(0==d)
            {
                throw new DivideByZeroException("Can't divide a vector by zero.");
            }

            return new Vector(v.X / d, v.Y / d);
        }

        public override string ToString()
        {
            return string.Format("({0:0.00},{1:0.00})", X, Y);
        }

        private static Random s_r = new Random();

        public static Vector RandomVector()
        {
            Vector v;

            do
            {
                v = new Vector(s_r.NextDouble(), s_r.NextDouble());
            } while (v.Length == 0);

            return v;
        }
    }
}
