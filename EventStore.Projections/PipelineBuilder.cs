namespace EventStore.Projections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class PipelineBuilder<T>
    {
        List<Func<IFilter<T>>> filters = new List<Func<IFilter<T>>>();

        public PipelineBuilder<T> Register(Func<IFilter<T>> filter)
        {
            filters.Add(filter);
            return this;
        }

        public PipelineBuilder<T> Register(IFilter<T> filter)
        {
            filters.Add(() => filter);
            return this;
        }

        public IFilter<T> Build()
        {
            var root = filters.First().Invoke();

            foreach (var filter in filters.Skip(1))
            {
                root.Register(filter.Invoke());
            }

            return root;
        }
    }
}