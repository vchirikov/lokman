using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Lokman.Client
{
    /// <summary>
    /// <see cref="EventCallback{T}"/> without <see cref="ComponentBase.StateHasChanged"/>
    /// Helps to use two-way binding with the same syntax as <see cref="EventCallback{T}"/>, but without double rendering
    /// of all child components in some cases (for example in cases with a changing route params)
    /// We can't use <see cref="EventCallbackFactory.Create"/> because of <c>CreateCore</c> implementation.
    /// Maybe I'll create issue for this
    /// <seealso href="https://github.com/SQL-MisterMagoo/BlazorEventsDemo" >More info</seealso>
    /// </summary>
    public static class EventCallbackFactoryExtensions
    {
        /// <inheritdoc cref="EventCallbackFactoryExtensions"/>
        public static EventCallback<TValue> CreateWithoutStateHasChanged<TValue>(this EventCallbackFactory? _, Action @delegate)
            => new EventCallback<TValue>(null, @delegate);

        /// <inheritdoc cref="EventCallbackFactoryExtensions"/>
        public static EventCallback<TValue> CreateWithoutStateHasChanged<TValue>(this EventCallbackFactory? _, Action<TValue> @delegate)
            => new EventCallback<TValue>(null, @delegate);

        /// <inheritdoc cref="EventCallbackFactoryExtensions"/>
        public static EventCallback<TValue> CreateWithoutStateHasChanged<TValue>(this EventCallbackFactory? _, Func<Task> @delegate)
            => new EventCallback<TValue>(null, @delegate);

        /// <inheritdoc cref="EventCallbackFactoryExtensions"/>
        public static EventCallback<TValue> CreateWithoutStateHasChanged<TValue>(this EventCallbackFactory? _, Func<TValue, Task> @delegate)
            => new EventCallback<TValue>(null, @delegate);

        /// <inheritdoc cref="EventCallbackFactoryExtensions"/>
        public static EventCallback<TValue> CreateWithoutStateHasChanged<TValue>(this EventCallbackFactory? _, MulticastDelegate @delegate)
            => new EventCallback<TValue>(null, @delegate);

        /// <inheritdoc cref="EventCallbackFactoryExtensions"/>
        public static EventCallback CreateWithoutStateHasChanged(this EventCallbackFactory? _, Action @delegate)
            => new EventCallback(null, @delegate);

        /// <inheritdoc cref="EventCallbackFactoryExtensions"/>
        public static EventCallback CreateWithoutStateHasChanged(this EventCallbackFactory? _, Func<Task> @delegate)
            => new EventCallback(null, @delegate);

        /// <inheritdoc cref="EventCallbackFactoryExtensions"/>
        public static EventCallback CreateWithoutStateHasChanged(this EventCallbackFactory? _, MulticastDelegate @delegate)
            => new EventCallback(null, @delegate);
    }
}
