﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Contrib.Monitoring.UI.Contracts;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Reactive.Samples
{
    public class GroupJoinLinq : SampleBase<Mood>
    {
        public override string Title => "Group Join (LINQ)";

        public override double Order => (double)SampleOrder.GroupJoinLinq;

        public override string Query
        {
            get
            {
                var query = @"IObservable<Weather> ws = ...;
IObservable<Mood> ms = ...;

var join = from w in ws
            join m in ms
                on ws   // a stream which close the weather period
                        // a stream which close the mood period (immediate closing)
                equals Observable.Empty<Unit>() // closing mood (point)
                into moods // the IObservable<Mood> which related to the current weather
            select new
                    {
                        Weather = w,
                        Moods = moods
                    };";
                return query;
            }
        }

        protected override IObservable<Mood> OnQuery()
        {
            #region IObservable<Weather> ws = ...

            IObservable<Weather> ws = Observable.Generate(0, i => i < 5, i => i + 1, i => (Weather)(i % 6),
                i => i == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(3));

            ws = ws.Monitor("Weather", Order);
            ws = ws.Publish().RefCount();

            #endregion // IObservable<Weather> ws = ...

            #region IObservable<Mood> ms = ...

            IObservable<Mood> ms = Observable.Generate(0, i => i < 15, i => i + 1, i => (Mood)(i % 6),
                i => i == 0 ? TimeSpan.FromSeconds(0.5) : TimeSpan.FromSeconds(1));

            ms = ms.Monitor("Moods", Order + 0.1);

            #endregion // IObservable<Mood> ms = ...

            var join = from w in ws.Seq()
                       join m in ms
                            on ws   // a stream which close the weather period
                                    // a stream which close the mood period (immediate closing)
                            equals Observable.Empty<Unit>() // closing mood (point)
                            into moods // the IObservable<Mood> which related to the current weather
                       select new
                                {
                                    Weather = w,
                                    Moods = moods
                                                 .Monitor(w.ToString(), Order + 0.4 + w.Index * 0.01)
                                };
 

            join = join.Monitor("Joined Weather", Order + 0.3, (t, m) => t.Weather.ToString());


            return join.SelectMany(m => m.Moods);
        }
    }
}
