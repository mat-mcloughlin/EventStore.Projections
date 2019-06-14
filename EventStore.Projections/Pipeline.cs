namespace EventStore.Projections
{
    using System.Threading.Tasks;

    public class Pipeline<T>
    {
        IFilter<T> _root;

        public Pipeline<T> Register(IFilter<T> filter)
        {
            if (_root == null)
            {
                _root = filter;
            }
            else
            {
                _root.Register(filter);
            }

            return this;
        }

        public Task Execute(T context)
        {
            return _root.Execute(context);
        }
    }
}