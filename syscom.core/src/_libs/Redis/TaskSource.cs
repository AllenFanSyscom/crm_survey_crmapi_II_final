using System.Threading.Tasks;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace libs.Redis
{
    /// <summary>
    /// We want to prevent callers hijacking the reader thread; this is a bit nasty, but works;
    /// see https://stackoverflow.com/a/22588431/23354 for more information; a huge
    /// thanks to Eli Arbel for spotting this (even though it is pure evil; it is *my kind of evil*)
    /// </summary>
    internal static class TaskSource
    {
        // on .NET < 4.6, it was possible to have threads hijacked; this is no longer a problem in 4.6 and core-clr 5,
        // thanks to the new TaskCreationOptions.RunContinuationsAsynchronously, however we still need to be a little
        // "test and react", as we could be targeting 4.5 but running on a 4.6 machine, in which case *it can still
        // work the magic* (thanks to over-the-top install)

        /// <summary>
        /// Indicates whether the specified task will not hijack threads when results are set
        /// </summary>
        public static readonly Func<Task, bool> IsSyncSafe;
		static TaskSource()
		{
            try
			{
				Type taskType = typeof(Task);
				FieldInfo continuationField = taskType.GetField("m_continuationObject", BindingFlags.Instance | BindingFlags.NonPublic);
				Type safeScenario = taskType.GetNestedType("SetOnInvokeMres", BindingFlags.NonPublic);
				if (continuationField != null && continuationField.FieldType == typeof(object) && safeScenario != null)
				{
					var method = new DynamicMethod("IsSyncSafe", typeof(bool), new[] { typeof(Task) }, typeof(Task), true);
					var il = method.GetILGenerator();
					//var hasContinuation = il.DefineLabel();
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldfld, continuationField);
					Label nonNull = il.DefineLabel(), goodReturn = il.DefineLabel();
					// check if null
					il.Emit(OpCodes.Brtrue_S, nonNull);
					il.MarkLabel(goodReturn);
					il.Emit(OpCodes.Ldc_I4_1);
					il.Emit(OpCodes.Ret);

					// check if is a SetOnInvokeMres - if so, we're OK
					il.MarkLabel(nonNull);
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldfld, continuationField);
					il.Emit(OpCodes.Isinst, safeScenario);
					il.Emit(OpCodes.Brtrue_S, goodReturn);

					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Ret);

					IsSyncSafe = (Func<Task, bool>)method.CreateDelegate(typeof(Func<Task, bool>));

					// and test them (check for an exception etc)
					var tcs = new TaskCompletionSource<int>();
					bool expectTrue = IsSyncSafe(tcs.Task);
					tcs.Task.ContinueWith(delegate { });
					bool expectFalse = IsSyncSafe(tcs.Task);
					tcs.SetResult(0);
					if (!expectTrue || expectFalse)
					{
						// revert to not trusting /them
						IsSyncSafe = null;
					}
				}
			}
			catch (Exception)
			{
				IsSyncSafe = null;
			}
            if (IsSyncSafe == null)
            {
                IsSyncSafe = _ => false; // assume: not
            }
        }

        /// <summary>
        /// Create a new TaskCompletion source
        /// </summary>
        /// <typeparam name="T">The type for the created <see cref="TaskCompletionSource{TResult}"/>.</typeparam>
        /// <param name="asyncState">The state for the created <see cref="TaskCompletionSource{TResult}"/>.</param>
        public static TaskCompletionSource<T> Create<T>(object asyncState)
        {
            return new TaskCompletionSource<T>(asyncState, TaskCreationOptions.None);
        }
    }
}
