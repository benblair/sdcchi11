using System.Collections.Generic;

namespace Cerrio.Samples.SDC
{
    public interface IPosititonable
    {
        double X { get; set;}
        double Y { get; set;}

        double Distance(IPosititonable item);

        IEnumerable<IPosititonable> Dependencies { get; }
    }
}
