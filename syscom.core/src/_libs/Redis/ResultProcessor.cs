using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace libs.Redis
{
    internal abstract class ResultProcessor
    {
        public static readonly ResultProcessor<bool>
            Boolean = new BooleanProcessor(),
            DemandOK = new ExpectBasicStringProcessor(RedisLiterals.BytesOK),
            DemandPONG = new ExpectBasicStringProcessor(RedisLiterals.BytesPONG),
            DemandZeroOrOne = new DemandZeroOrOneProcessor(),
            AutoConfigure = new AutoConfigureProcessor(),
            TrackSubscriptions = new TrackSubscriptionsProcessor(),
            Tracer = new TracerProcessor(false),
            EstablishConnection = new TracerProcessor(true),
            BackgroundSaveStarted = new ExpectBasicStringProcessor(RedisLiterals.BytesBackgroundSavingStarted);

        public static readonly ResultProcessor<byte[]>
            ByteArray = new ByteArrayProcessor(),
            ScriptLoad = new ScriptLoadProcessor();

        public static readonly ResultProcessor<ClusterConfiguration>
            ClusterNodes = new ClusterNodesProcessor();

        public static readonly ResultProcessor<EndPoint>
            ConnectionIdentity = new ConnectionIdentityProcessor();

        public static readonly ResultProcessor<DateTime>
            DateTime = new DateTimeProcessor();

        public static readonly ResultProcessor<double>
                                            Double = new DoubleProcessor();
        public static readonly ResultProcessor<IGrouping<string, KeyValuePair<string, string>>[]>
            Info = new InfoProcessor();

        public static readonly MultiStreamProcessor
            MultiStream = new MultiStreamProcessor();

        public static readonly ResultProcessor<long>
            Int64 = new Int64Processor(),
            PubSubNumSub = new PubSubNumSubProcessor();

        public static readonly ResultProcessor<double?>
                            NullableDouble = new NullableDoubleProcessor();
        public static readonly ResultProcessor<long?>
            NullableInt64 = new NullableInt64Processor();

        public static readonly ResultProcessor<RedisChannel[]>
            RedisChannelArrayLiteral = new RedisChannelArrayProcessor(RedisChannel.PatternMode.Literal);

        public static readonly ResultProcessor<RedisKey>
                    RedisKey = new RedisKeyProcessor();

        public static readonly ResultProcessor<RedisKey[]>
            RedisKeyArray = new RedisKeyArrayProcessor();

        public static readonly ResultProcessor<RedisType>
            RedisType = new RedisTypeProcessor();

        public static readonly ResultProcessor<RedisValue>
            RedisValue = new RedisValueProcessor();

        public static readonly ResultProcessor<RedisValue[]>
            RedisValueArray = new RedisValueArrayProcessor();

        public static readonly ResultProcessor<string[]>
            StringArray = new StringArrayProcessor();

        public static readonly ResultProcessor<GeoPosition?[]>
            RedisGeoPositionArray = new RedisValueGeoPositionArrayProcessor();
        public static readonly ResultProcessor<GeoPosition?>
            RedisGeoPosition = new RedisValueGeoPositionProcessor();

        public static readonly ResultProcessor<TimeSpan>
            ResponseTimer = new TimingProcessor();

        public static readonly ResultProcessor<RedisResult>
            ScriptResult = new ScriptResultProcessor();

        public static readonly SortedSetEntryArrayProcessor
            SortedSetWithScores = new SortedSetEntryArrayProcessor();

        public static readonly SingleStreamProcessor
            SingleStream = new SingleStreamProcessor();

        public static readonly SingleStreamProcessor
            SingleStreamWithNameSkip = new SingleStreamProcessor(skipStreamName: true);

        public static readonly StreamConsumerInfoProcessor
            StreamConsumerInfo = new StreamConsumerInfoProcessor();

        public static readonly StreamGroupInfoProcessor
            StreamGroupInfo = new StreamGroupInfoProcessor();

        public static readonly StreamInfoProcessor
            StreamInfo = new StreamInfoProcessor();

        public static readonly StreamPendingInfoProcessor
            StreamPendingInfo = new StreamPendingInfoProcessor();

        public static readonly StreamPendingMessagesProcessor
            StreamPendingMessages = new StreamPendingMessagesProcessor();

        public static ResultProcessor<GeoRadiusResult[]> GeoRadiusArray(GeoRadiusOptions options) => GeoRadiusResultArrayProcessor.Get(options);

        public static readonly ResultProcessor<string>
                            String = new StringProcessor(),
            ClusterNodesRaw = new ClusterNodesRawProcessor();

        #region Sentinel

        public static readonly ResultProcessor<EndPoint>
            SentinelMasterEndpoint = new SentinelGetMasterAddressByNameProcessor();

        public static readonly ResultProcessor<KeyValuePair<string, string>[][]>
            SentinelArrayOfArrays = new SentinelArrayOfArraysProcessor();

        #endregion

        public static readonly ResultProcessor<KeyValuePair<string, string>[]>
            StringPairInterleaved = new StringPairInterleavedProcessor();
        public static readonly TimeSpanProcessor
            TimeSpanFromMilliseconds = new TimeSpanProcessor(true),
            TimeSpanFromSeconds = new TimeSpanProcessor(false);
        public static readonly HashEntryArrayProcessor
            HashEntryArray = new HashEntryArrayProcessor();
        private static readonly byte[] MOVED = Encoding.UTF8.GetBytes("MOVED "), ASK = Encoding.UTF8.GetBytes("ASK ");

        public void ConnectionFail(Message message, ConnectionFailureType fail, Exception innerException)
        {
            PhysicalConnection.IdentifyFailureType(innerException, ref fail);

            string exMessage = fail.ToString() + (message == null ? "" : (" on " + message.Command));
            var ex = innerException == null ? new RedisConnectionException(fail, exMessage)
                : new RedisConnectionException(fail, exMessage, innerException);
            SetException(message, ex);
        }

        public void ConnectionFail(Message message, ConnectionFailureType fail, string errorMessage)
        {
            SetException(message, new RedisConnectionException(fail, errorMessage));
        }

        public void ServerFail(Message message, string errorMessage)
        {
            SetException(message, new RedisServerException(errorMessage));
        }

        public void SetException(Message message, Exception ex)
        {
            var box = message?.ResultBox;
            box?.SetException(ex);
        }
        // true if ready to be completed (i.e. false if re-issued to another server)
        public virtual bool SetResult(PhysicalConnection connection, Message message, RawResult result)
        {
            if (message is LoggingMessage logging)
            {
                try
                {
                    connection.Multiplexer.LogLocked(logging.Log, "Response from {0} / {1}: {2}", connection.Bridge, message.CommandAndKey, result);
                }
                catch { }
            }
            if (result.IsError)
            {
                var bridge = connection.Bridge;
                var server = bridge.ServerEndPoint;
                bool log = !message.IsInternalCall;
                bool isMoved = result.AssertStarts(MOVED);
                string err = string.Empty;
                bool unableToConnectError = false;
                if (isMoved || result.AssertStarts(ASK))
                {
                    message.SetResponseReceived();

                    log = false;
                    string[] parts = result.GetString().Split(StringSplits.Space, 3);
                    EndPoint endpoint;
                    if (Format.TryParseInt32(parts[1], out int hashSlot)
                        && (endpoint = Format.TryParseEndPoint(parts[2])) != null)
                    {
                        // no point sending back to same server, and no point sending to a dead server
                        if (!Equals(server.EndPoint, endpoint))
                        {
                            if (bridge.Multiplexer.TryResend(hashSlot, message, endpoint, isMoved))
                            {
                                connection.Multiplexer.Trace(message.Command + " re-issued to " + endpoint, isMoved ? "MOVED" : "ASK");
                                return false;
                            }
                            else
                            {
                                if (isMoved && (message.Flags & CommandFlags.NoRedirect) != 0)
                                {
                                    err = $"Key has MOVED from Endpoint {endpoint} and hashslot {hashSlot} but CommandFlags.NoRedirect was specified - redirect not followed. ";
                                }
                                else
                                {
                                    unableToConnectError = true;
                                    err = $"Endpoint {endpoint} serving hashslot {hashSlot} is not reachable at this point of time. Please check connectTimeout value. If it is low, try increasing it to give the ConnectionMultiplexer a chance to recover from the network disconnect.  ";
                                }
#if FEATURE_PERFCOUNTER
                                err += ConnectionMultiplexer.GetThreadPoolAndCPUSummary(bridge.Multiplexer.IncludePerformanceCountersInExceptions);
#endif
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(err))
                {
                    err = result.GetString();
                }

                if (log)
                {
                    bridge.Multiplexer.OnErrorMessage(server.EndPoint, err);
                }
                connection.Multiplexer.Trace("Completed with error: " + err + " (" + GetType().Name + ")", ToString());
                if (unableToConnectError)
                {
                    ConnectionFail(message, ConnectionFailureType.UnableToConnect, err);
                }
                else
                {
                    ServerFail(message, err);
                }
            }
            else
            {
                bool coreResult = SetResultCore(connection, message, result);
                if (coreResult)
                {
                    connection.Multiplexer.Trace("Completed with success: " + result.ToString() + " (" + GetType().Name + ")", ToString());
                }
                else
                {
                    UnexpectedResponse(message, result);
                }
            }
            return true;
        }

        protected abstract bool SetResultCore(PhysicalConnection connection, Message message, RawResult result);

        private void UnexpectedResponse(Message message, RawResult result)
        {
            ConnectionMultiplexer.TraceWithoutContext("From " + GetType().Name, "Unexpected Response");
            ConnectionFail(message, ConnectionFailureType.ProtocolFailure, "Unexpected response to " + (message?.Command.ToString() ?? "n/a") + ": " + result.ToString());
        }

        public sealed class TimeSpanProcessor : ResultProcessor<TimeSpan?>
        {
            private readonly bool isMilliseconds;
            public TimeSpanProcessor(bool isMilliseconds)
            {
                this.isMilliseconds = isMilliseconds;
            }

            public bool TryParse(RawResult result, out TimeSpan? expiry)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                        long time;
                        if (result.TryGetInt64(out time))
                        {
                            if (time < 0)
                            {
                                expiry = null;
                            }
                            else if (isMilliseconds)
                            {
                                expiry = TimeSpan.FromMilliseconds(time);
                            }
                            else
                            {
                                expiry = TimeSpan.FromSeconds(time);
                            }
                            return true;
                        }
                        break;
                }
                expiry = null;
                return false;
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (TryParse(result, out TimeSpan? expiry))
                {
                    SetResult(message, expiry);
                    return true;
                }
                return false;
            }
        }

        public sealed class TimingProcessor : ResultProcessor<TimeSpan>
        {
            public static TimerMessage CreateMessage(int db, CommandFlags flags, RedisCommand command, RedisValue value = default(RedisValue))
            {
                return new TimerMessage(db, flags, command, value);
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.Type == ResultType.Error)
                {
                    return false;
                }
                else
                {   // don't check the actual reply; there are multiple ways of constructing
                    // a timing message, and we don't actually care about what approach was used
                    TimeSpan duration;
                    if (message is TimerMessage timingMessage)
                    {
                        var watch = timingMessage.Watch;
                        watch.Stop();
                        duration = watch.Elapsed;
                    }
                    else
                    {
                        duration = TimeSpan.MaxValue;
                    }
                    SetResult(message, duration);
                    return true;
                }
            }

            internal sealed class TimerMessage : Message
            {
                public readonly Stopwatch Watch;
                private readonly RedisValue value;
                public TimerMessage(int db, CommandFlags flags, RedisCommand command, RedisValue value)
                    : base(db, flags, command)
                {
                    Watch = Stopwatch.StartNew();
                    this.value = value;
                }

                internal override void WriteImpl(PhysicalConnection physical)
                {
                    if (value.IsNull)
                    {
                        physical.WriteHeader(command, 0);
                    }
                    else
                    {
                        physical.WriteHeader(command, 1);
                        physical.Write(value);
                    }
                }
            }
        }

        public sealed class TrackSubscriptionsProcessor : ResultProcessor<bool>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.Type == ResultType.MultiBulk)
                {
                    var items = result.GetItems();
                    if (items.Length >= 3 && items[2].TryGetInt64(out long count))
                    {
                        connection.SubscriptionCount = count;
                        return true;
                    }
                }
                return false;
            }
        }

        internal sealed class DemandZeroOrOneProcessor : ResultProcessor<bool>
        {
            private static readonly byte[] zero = { (byte)'0' }, one = { (byte)'1' };

            public static bool TryGet(RawResult result, out bool value)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        if (result.IsEqual(one)) { value = true; return true; }
                        else if (result.IsEqual(zero)) { value = false; return true; }
                        break;
                }
                value = false;
                return false;
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (TryGet(result, out bool value))
                {
                    SetResult(message, value);
                    return true;
                }
                return false;
            }
        }

        internal sealed class ScriptLoadProcessor : ResultProcessor<byte[]>
        {
            private static readonly Regex sha1 = new Regex("^[0-9a-f]{40}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            internal static bool IsSHA1(string script)
            {
                return script != null && sha1.IsMatch(script);
            }

            internal static byte[] ParseSHA1(byte[] value)
            {
                if (value?.Length == 40)
                {
                    var tmp = new byte[20];
                    int charIndex = 0;
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        int x = FromHex((char)value[charIndex++]), y = FromHex((char)value[charIndex++]);
                        if (x < 0 || y < 0) return null;
                        tmp[i] = (byte)((x << 4) | y);
                    }
                    return tmp;
                }
                return null;
            }

            internal static byte[] ParseSHA1(string value)
            {
                if (value?.Length == 40 && sha1.IsMatch(value))
                {
                    var tmp = new byte[20];
                    int charIndex = 0;
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        int x = FromHex(value[charIndex++]), y = FromHex(value[charIndex++]);
                        if (x < 0 || y < 0) return null;
                        tmp[i] = (byte)((x << 4) | y);
                    }
                    return tmp;
                }
                return null;
            }

            private static int FromHex(char c)
            {
                if (c >= '0' && c <= '9') return c - '0';
                if (c >= 'a' && c <= 'f') return c - 'a' + 10;
                if (c >= 'A' && c <= 'F') return c - 'A' + 10;
                return -1;
            }
            // note that top-level error messages still get handled by SetResult, but nested errors
            // (is that a thing?) will be wrapped in the RedisResult
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.BulkString:
                        var asciiHash = result.GetBlob();
                        if (asciiHash == null || asciiHash.Length != 40) return false;

                        byte[] hash = null;
                        if (!message.IsInternalCall)
                        {
                            hash = ParseSHA1(asciiHash); // external caller wants the hex bytes, not the ascii bytes
                        }
                        var sl = message as RedisDatabase.ScriptLoadMessage;
                        if (sl != null)
                        {
                            connection.Bridge.ServerEndPoint.AddScript(sl.Script, asciiHash);
                        }
                        SetResult(message, hash);
                        return true;
                }
                return false;
            }
        }

        internal sealed class SortedSetEntryArrayProcessor : ValuePairInterleavedProcessorBase<SortedSetEntry>
        {
            protected override SortedSetEntry Parse(RawResult first, RawResult second)
            {
                return new SortedSetEntry(first.AsRedisValue(), second.TryGetDouble(out double val) ? val : double.NaN);
            }
        }

        internal sealed class HashEntryArrayProcessor : ValuePairInterleavedProcessorBase<HashEntry>
        {
            protected override HashEntry Parse(RawResult first, RawResult second)
            {
                return new HashEntry(first.AsRedisValue(), second.AsRedisValue());
            }
        }

        internal abstract class ValuePairInterleavedProcessorBase<T> : ResultProcessor<T[]>
        {
            private static readonly T[] nix = new T[0];

            public bool TryParse(RawResult result, out T[] pairs)
            {
                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var arr = result.GetItems();
                        if (arr == null)
                        {
                            pairs = null;
                        }
                        else
                        {
                            int count = arr.Length / 2;
                            if (count == 0)
                            {
                                pairs = nix;
                            }
                            else
                            {
                                pairs = new T[count];
                                int offset = 0;
                                for (int i = 0; i < pairs.Length; i++)
                                {
                                    pairs[i] = Parse(arr[offset++], arr[offset++]);
                                }
                            }
                        }
                        return true;
                    default:
                        pairs = null;
                        return false;
                }
            }

            protected abstract T Parse(RawResult first, RawResult second);
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (TryParse(result, out T[] arr))
                {
                    SetResult(message, arr);
                    return true;
                }
                return false;
            }
        }

        private sealed class AutoConfigureProcessor : ResultProcessor<bool>
        {
            private static readonly byte[] READONLY = Encoding.UTF8.GetBytes("READONLY ");
            public override bool SetResult(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.IsError && result.AssertStarts(READONLY))
                {
                    var server = connection.Bridge.ServerEndPoint;
                    server.Multiplexer.Trace("Auto-configured role: slave");
                    server.IsSlave = true;
                }
                return base.SetResult(connection, message, result);
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                var server = connection.Bridge.ServerEndPoint;
                switch (result.Type)
                {
                    case ResultType.BulkString:
                        if (message?.Command == RedisCommand.INFO)
                        {
                            string info = result.GetString(), line;
                            if (string.IsNullOrWhiteSpace(info))
                            {
                                SetResult(message, true);
                                return true;
                            }
                            string? masterHost = null, masterPort = null;
                            bool roleSeen = false;
                            using (var reader = new StringReader(info))
                            {
                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("# ")) continue;

                                    string val;
                                    if ((val = Extract(line, "role:")) != null)
                                    {
                                        roleSeen = true;
                                        switch (val)
                                        {
                                            case "master":
                                                server.IsSlave = false;
                                                server.Multiplexer.Trace("Auto-configured role: master");
                                                break;
                                            case "slave":
                                                server.IsSlave = true;
                                                server.Multiplexer.Trace("Auto-configured role: slave");
                                                break;
                                        }
                                    }
                                    else if ((val = Extract(line, "master_host:")) != null)
                                    {
                                        masterHost = val;
                                    }
                                    else if ((val = Extract(line, "master_port:")) != null)
                                    {
                                        masterPort = val;
                                    }
                                    else if ((val = Extract(line, "redis_version:")) != null)
                                    {
                                        if (Version.TryParse(val, out Version version))
                                        {
                                            server.Version = version;
                                            server.Multiplexer.Trace("Auto-configured version: " + version);
                                        }
                                    }
                                    else if ((val = Extract(line, "redis_mode:")) != null)
                                    {
                                        switch (val)
                                        {
                                            case "standalone":
                                                server.ServerType = ServerType.Standalone;
                                                server.Multiplexer.Trace("Auto-configured server-type: standalone");
                                                break;
                                            case "cluster":
                                                server.ServerType = ServerType.Cluster;
                                                server.Multiplexer.Trace("Auto-configured server-type: cluster");
                                                break;
                                            case "sentinel":
                                                server.ServerType = ServerType.Sentinel;
                                                server.Multiplexer.Trace("Auto-configured server-type: sentinel");
                                                break;
                                        }
                                    }
                                    else if ((val = Extract(line, "run_id:")) != null)
                                    {
                                        server.RunId = val;
                                    }
                                }
                                if (roleSeen)
                                { // these are in the same section, if presnt
                                    server.MasterEndPoint = Format.TryParseEndPoint(masterHost, masterPort);
                                }
                            }
                        }
                        SetResult(message, true);
                        return true;
                    case ResultType.MultiBulk:
                        if (message?.Command == RedisCommand.CONFIG)
                        {
                            var arr = result.GetItems();
                            int count = arr.Length / 2;

                            byte[] timeout = (byte[])RedisLiterals.timeout,
                                databases = (byte[])RedisLiterals.databases,
                                slave_read_only = (byte[])RedisLiterals.slave_read_only,
                                yes = (byte[])RedisLiterals.yes,
                                no = (byte[])RedisLiterals.no;

                            for (int i = 0; i < count; i++)
                            {
                                var key = arr[i * 2];
                                if (key.IsEqual(timeout) && arr[(i * 2) + 1].TryGetInt64(out long i64))
                                {
                                    // note the configuration is in seconds
                                    int timeoutSeconds = checked((int)i64), targetSeconds;
                                    if (timeoutSeconds > 0)
                                    {
                                        if (timeoutSeconds >= 60)
                                        {
                                            targetSeconds = timeoutSeconds - 20; // time to spare...
                                        }
                                        else
                                        {
                                            targetSeconds = (timeoutSeconds * 3) / 4;
                                        }
                                        server.Multiplexer.Trace("Auto-configured timeout: " + targetSeconds + "s");
                                        server.WriteEverySeconds = targetSeconds;
                                    }
                                }
                                else if (key.IsEqual(databases) && arr[(i * 2) + 1].TryGetInt64(out i64))
                                {
                                    int dbCount = checked((int)i64);
                                    server.Multiplexer.Trace("Auto-configured databases: " + dbCount);
                                    server.Databases = dbCount;
                                }
                                else if (key.IsEqual(slave_read_only))
                                {
                                    var val = arr[(i * 2) + 1];
                                    if (val.IsEqual(yes))
                                    {
                                        server.SlaveReadOnly = true;
                                        server.Multiplexer.Trace("Auto-configured slave-read-only: true");
                                    }
                                    else if (val.IsEqual(no))
                                    {
                                        server.SlaveReadOnly = false;
                                        server.Multiplexer.Trace("Auto-configured slave-read-only: false");
                                    }
                                }
                            }
                        }
                        SetResult(message, true);
                        return true;
                }
                return false;
            }

            private static string Extract(string line, string prefix)
            {
                if (line.StartsWith(prefix)) return line.Substring(prefix.Length).Trim();
                return null;
            }
        }

        private sealed class BooleanProcessor : ResultProcessor<bool>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.IsNull)
                {
                    SetResult(message, false); // lots of ops return (nil) when they mean "no"
                    return true;
                }
                switch (result.Type)
                {
                    case ResultType.SimpleString:
                        if (result.IsEqual(RedisLiterals.BytesOK))
                        {
                            SetResult(message, true);
                        }
                        else
                        {
                            SetResult(message, result.GetBoolean());
                        }
                        return true;
                    case ResultType.Integer:
                    case ResultType.BulkString:
                        SetResult(message, result.GetBoolean());
                        return true;
                    case ResultType.MultiBulk:
                        var items = result.GetItems();
                        if (items.Length == 1)
                        { // treat an array of 1 like a single reply (for example, SCRIPT EXISTS)
                            SetResult(message, items[0].GetBoolean());
                            return true;
                        }
                        break;
                }
                return false;
            }
        }

        private sealed class ByteArrayProcessor : ResultProcessor<byte[]>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.BulkString:
                        SetResult(message, result.GetBlob());
                        return true;
                }
                return false;
            }
        }

        private sealed class ClusterNodesProcessor : ResultProcessor<ClusterConfiguration>
        {
            internal static ClusterConfiguration Parse(PhysicalConnection connection, string nodes)
            {
                var server = connection.Bridge.ServerEndPoint;
                var config = new ClusterConfiguration(connection.Multiplexer.ServerSelectionStrategy, nodes, server.EndPoint);
                server.SetClusterConfiguration(config);
                return config;
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.BulkString:
                        string nodes = result.GetString();
                        connection.Bridge.ServerEndPoint.ServerType = ServerType.Cluster;
                        var config = Parse(connection, nodes);
                        SetResult(message, config);
                        return true;
                }
                return false;
            }
        }

        private sealed class ClusterNodesRawProcessor : ResultProcessor<string>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        string nodes = result.GetString();
                        try
                        { ClusterNodesProcessor.Parse(connection, nodes); }
                        catch
                        { /* tralalalala */}
                        SetResult(message, nodes);
                        return true;
                }
                return false;
            }
        }

        private sealed class ConnectionIdentityProcessor : ResultProcessor<EndPoint>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                SetResult(message, connection.Bridge.ServerEndPoint.EndPoint);
                return true;
            }
        }

        private sealed class DateTimeProcessor : ResultProcessor<DateTime>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                long unixTime;
                switch (result.Type)
                {
                    case ResultType.Integer:
                        if (result.TryGetInt64(out unixTime))
                        {
                            var time = RedisBase.UnixEpoch.AddSeconds(unixTime);
                            SetResult(message, time);
                            return true;
                        }
                        break;
                    case ResultType.MultiBulk:
                        var arr = result.GetItems();
                        switch (arr.Length)
                        {
                            case 1:
                                if (arr[0].TryGetInt64(out unixTime))
                                {
                                    var time = RedisBase.UnixEpoch.AddSeconds(unixTime);
                                    SetResult(message, time);
                                    return true;
                                }
                                break;
                            case 2:
                                if (arr[0].TryGetInt64(out unixTime) && arr[1].TryGetInt64(out long micros))
                                {
                                    var time = RedisBase.UnixEpoch.AddSeconds(unixTime).AddTicks(micros * 10); // datetime ticks are 100ns
                                    SetResult(message, time);
                                    return true;
                                }
                                break;
                        }
                        break;
                }
                return false;
            }
        }

        private sealed class DoubleProcessor : ResultProcessor<double>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                        long i64;
                        if (result.TryGetInt64(out i64))
                        {
                            SetResult(message, i64);
                            return true;
                        }
                        break;
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        double val;
                        if (result.TryGetDouble(out val))
                        {
                            SetResult(message, val);
                            return true;
                        }
                        break;
                }
                return false;
            }
        }

        private sealed class ExpectBasicStringProcessor : ResultProcessor<bool>
        {
            private readonly byte[] expected;
            public ExpectBasicStringProcessor(string value)
            {
                expected = Encoding.UTF8.GetBytes(value);
            }

            public ExpectBasicStringProcessor(byte[] value)
            {
                expected = value;
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.IsEqual(expected))
                {
                    SetResult(message, true);
                    return true;
                }
                return false;
            }
        }

        private sealed class InfoProcessor : ResultProcessor<IGrouping<string, KeyValuePair<string, string>>[]>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.Type == ResultType.BulkString)
                {
                    string category = Normalize(null), line;
                    var list = new List<Tuple<string, KeyValuePair<string, string>>>();
                    using (var reader = new StringReader(result.GetString()))
                    {
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            if (line.StartsWith("# "))
                            {
                                category = Normalize(line.Substring(2));
                                continue;
                            }
                            int idx = line.IndexOf(':');
                            if (idx < 0) continue;
                            var pair = new KeyValuePair<string, string>(
                                line.Substring(0, idx).Trim(),
                                line.Substring(idx + 1).Trim());
                            list.Add(Tuple.Create(category, pair));
                        }
                    }
                    var final = list.GroupBy(x => x.Item1, x => x.Item2).ToArray();
                    SetResult(message, final);
                    return true;
                }
                return false;
            }

            private static string Normalize(string category)
            {
                return string.IsNullOrWhiteSpace(category) ? "miscellaneous" : category.Trim();
            }
        }

        private class Int64Processor : ResultProcessor<long>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        long i64;
                        if (result.TryGetInt64(out i64))
                        {
                            SetResult(message, i64);
                            return true;
                        }
                        break;
                }
                return false;
            }
        }

        private class PubSubNumSubProcessor : Int64Processor
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.Type == ResultType.MultiBulk)
                {
                    var arr = result.GetItems();
                    if (arr?.Length == 2 && arr[1].TryGetInt64(out long val))
                    {
                        SetResult(message, val);
                        return true;
                    }
                }
                return base.SetResultCore(connection, message, result);
            }
        }

        private sealed class NullableDoubleProcessor : ResultProcessor<double?>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        if (result.IsNull)
                        {
                            SetResult(message, null);
                            return true;
                        }
                        double val;
                        if (result.TryGetDouble(out val))
                        {
                            SetResult(message, val);
                            return true;
                        }
                        break;
                }
                return false;
            }
        }

        private sealed class NullableInt64Processor : ResultProcessor<long?>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        if (result.IsNull)
                        {
                            SetResult(message, null);
                            return true;
                        }
                        long i64;
                        if (result.TryGetInt64(out i64))
                        {
                            SetResult(message, i64);
                            return true;
                        }
                        break;
                }
                return false;
            }
        }

        private sealed class RedisChannelArrayProcessor : ResultProcessor<RedisChannel[]>
        {
            private readonly RedisChannel.PatternMode mode;
            public RedisChannelArrayProcessor(RedisChannel.PatternMode mode)
            {
                this.mode = mode;
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var arr = result.GetItems();
                        RedisChannel[] final;
                        if (arr.Length == 0)
                        {
                            final = RedisChannel.EmptyArray;
                        }
                        else
                        {
                            final = new RedisChannel[arr.Length];
                            byte[] channelPrefix = connection.ChannelPrefix;
                            for (int i = 0; i < final.Length; i++)
                            {
                                final[i] = arr[i].AsRedisChannel(channelPrefix, mode);
                            }
                        }
                        SetResult(message, final);
                        return true;
                }
                return false;
            }
        }

        private sealed class RedisKeyArrayProcessor : ResultProcessor<RedisKey[]>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var arr = result.GetItemsAsKeys();
                        SetResult(message, arr);
                        return true;
                }
                return false;
            }
        }

        private sealed class RedisKeyProcessor : ResultProcessor<RedisKey>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        SetResult(message, result.AsRedisKey());
                        return true;
                }
                return false;
            }
        }

        private sealed class RedisTypeProcessor : ResultProcessor<RedisType>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        string s = result.GetString();
                        RedisType value;
                        if (string.Equals(s, "zset", StringComparison.OrdinalIgnoreCase)) value = Redis.RedisType.SortedSet;
                        else if (!Enum.TryParse<RedisType>(s, true, out value)) value = global::libs.Redis.RedisType.Unknown;
                        SetResult(message, value);
                        return true;
                }
                return false;
            }
        }

        private sealed class RedisValueArrayProcessor : ResultProcessor<RedisValue[]>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var arr = result.GetItemsAsValues();

                        SetResult(message, arr);
                        return true;
                }
                return false;
            }
        }

        private sealed class StringArrayProcessor : ResultProcessor<string[]>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var arr = result.GetItemsAsStrings();

                        SetResult(message, arr);
                        return true;
                }
                return false;
            }
        }

        private sealed class RedisValueGeoPositionProcessor : ResultProcessor<GeoPosition?>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var pos = result.GetItemsAsGeoPosition();

                        SetResult(message, pos);
                        return true;
                }
                return false;
            }
        }

        private sealed class RedisValueGeoPositionArrayProcessor : ResultProcessor<GeoPosition?[]>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var arr = result.GetItemsAsGeoPositionArray();

                        SetResult(message, arr);
                        return true;
                }
                return false;
            }
        }

        private sealed class GeoRadiusResultArrayProcessor : ResultProcessor<GeoRadiusResult[]>
        {
            private static readonly GeoRadiusResultArrayProcessor[] instances;
            private readonly GeoRadiusOptions options;

            static GeoRadiusResultArrayProcessor()
            {
                instances = new GeoRadiusResultArrayProcessor[8];
                for (int i = 0; i < 8; i++) instances[i] = new GeoRadiusResultArrayProcessor((GeoRadiusOptions)i);
            }

            public static GeoRadiusResultArrayProcessor Get(GeoRadiusOptions options)
            {
                int i = (int)options;
                if (i < 0 || i >= instances.Length) throw new ArgumentOutOfRangeException(nameof(options));
                return instances[i];
            }

            private GeoRadiusResultArrayProcessor(GeoRadiusOptions options)
            {
                this.options = options;
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var arr = result.GetItemsAsRawResults();

                        GeoRadiusResult[] typed;
                        if (arr == null)
                        {
                            typed = null;
                        }
                        else
                        {
                            var options = this.options;
                            typed = new GeoRadiusResult[arr.Length];
                            for (int i = 0; i < arr.Length; i++)
                            {
                                typed[i] = Parse(options, arr[i]);
                            }
                        }
                        SetResult(message, typed);
                        return true;
                }
                return false;
            }

            private static GeoRadiusResult Parse(GeoRadiusOptions options, RawResult item)
            {
                if (options == GeoRadiusOptions.None)
                {
                    // Without any WITH option specified, the command just returns a linear array like ["New York","Milan","Paris"].
                    return new GeoRadiusResult(item.AsRedisValue(), null, null, null);
                }
                // If WITHCOORD, WITHDIST or WITHHASH options are specified, the command returns an array of arrays, where each sub-array represents a single item.
                var arr = item.GetArrayOfRawResults();

                int index = 0;
                // the first item in the sub-array is always the name of the returned item.
                var member = arr[index++].AsRedisValue();

                /*  The other information is returned in the following order as successive elements of the sub-array.
The distance from the center as a floating point number, in the same unit specified in the radius.
The geohash integer.
The coordinates as a two items x,y array (longitude,latitude).
                 */
                double? distance = null;
                GeoPosition? position = null;
                long? hash = null;
                if ((options & GeoRadiusOptions.WithDistance) != 0) { distance = (double?)arr[index++].AsRedisValue(); }
                if ((options & GeoRadiusOptions.WithGeoHash) != 0) { hash = (long?)arr[index++].AsRedisValue(); }
                if ((options & GeoRadiusOptions.WithCoordinates) != 0)
                {
                    var coords = arr[index++].GetArrayOfRawResults();
                    double longitude = (double)coords[0].AsRedisValue(), latitude = (double)coords[1].AsRedisValue();
                    position = new GeoPosition(longitude, latitude);
                }
                return new GeoRadiusResult(member, distance, hash, position);
            }
        }

        private sealed class RedisValueProcessor : ResultProcessor<RedisValue>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        SetResult(message, result.AsRedisValue());
                        return true;
                }
                return false;
            }
        }

        private class ScriptResultProcessor : ResultProcessor<RedisResult>
        {
            private static readonly byte[] NOSCRIPT = Encoding.UTF8.GetBytes("NOSCRIPT ");
            public override bool SetResult(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.Type == ResultType.Error && result.AssertStarts(NOSCRIPT))
                { // scripts are not flushed individually, so assume the entire script cache is toast ("SCRIPT FLUSH")
                    connection.Bridge.ServerEndPoint.FlushScriptCache();
                    message.SetScriptUnavailable();
                }
                // and apply usual processing for the rest
                return base.SetResult(connection, message, result);
            }

            // note that top-level error messages still get handled by SetResult, but nested errors
            // (is that a thing?) will be wrapped in the RedisResult
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                var value = Redis.RedisResult.TryCreate(connection, result);
                if (value != null)
                {
                    SetResult(message, value);
                    return true;
                }
                return false;
            }
        }

        internal sealed class SingleStreamProcessor : StreamProcessorBase<StreamEntry[]>
        {
            private bool skipStreamName;

            public SingleStreamProcessor(bool skipStreamName = false)
            {
                this.skipStreamName = skipStreamName;
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.IsNull)
                {
                    // Server returns 'nil' if no entries are returned for the given stream.
                    SetResult(message, new StreamEntry[0]);
                    return true;
                }

                if (result.Type != ResultType.MultiBulk)
                {
                    return false;
                }

                StreamEntry[]? entries = null;

                if (skipStreamName)
                {
                    // Skip the first element in the array (i.e., the stream name).
                    // See https://redis.io/commands/xread.

                    // > XREAD COUNT 2 STREAMS mystream 0
                    // 1) 1) "mystream"                     <== Skip the stream name
                    //    2) 1) 1) 1519073278252 - 0        <== Index 1 contains the array of stream entries
                    //          2) 1) "foo"
                    //             2) "value_1"
                    //       2) 1) 1519073279157 - 0
                    //          2) 1) "foo"
                    //             2) "value_2"

                    // Retrieve the initial array. For XREAD of a single stream it will
                    // be an array of only 1 element in the response.
                    var readResult = result.GetItems();

                    // Within that single element, GetItems will return an array of
                    // 2 elements: the stream name and the stream entries.
                    // Skip the stream name (index 0) and only process the stream entries (index 1).
                    entries = ParseRedisStreamEntries(readResult[0].GetItems()[1]);
                }
                else
                {
                    entries = ParseRedisStreamEntries(result);
                }

                SetResult(message, entries);
                return true;
            }
        }

        internal sealed class MultiStreamProcessor : StreamProcessorBase<RedisStream[]>
        {
            /*
                The result is similar to the XRANGE result (see SingleStreamProcessor)
                with the addition of the stream name as the first element of top level
                Multibulk array.

                See https://redis.io/commands/xread.

                > XREAD COUNT 2 STREAMS mystream writers 0-0 0-0
                1) 1) "mystream"
                   2) 1) 1) 1526984818136-0
                         2) 1) "duration"
                            2) "1532"
                            3) "event-id"
                            4) "5"
                      2) 1) 1526999352406-0
                         2) 1) "duration"
                            2) "812"
                            3) "event-id"
                            4) "9"
                2) 1) "writers"
                   2) 1) 1) 1526985676425-0
                         2) 1) "name"
                            2) "Virginia"
                            3) "surname"
                            4) "Woolf"
                      2) 1) 1526985685298-0
                         2) 1) "name"
                            2) "Jane"
                            3) "surname"
                            4) "Austen"
            */

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.IsNull)
                {
                    // Nothing returned for any of the requested streams. The server returns 'nil'.
                    SetResult(message, new RedisStream[0]);
                    return true;
                }

                if (result.Type != ResultType.MultiBulk)
                {
                    return false;
                }

                var arr = result.GetItems();

                var streams = Array.ConvertAll(arr, item =>
                {
                    var details = item.GetItems();

                    // details[0] = Name of the Stream
                    // details[1] = Multibulk Array of Stream Entries

                    return new RedisStream(key: details[0].AsRedisKey(),
                        entries: ParseRedisStreamEntries(details[1]));
                });

                SetResult(message, streams);
                return true;
            }
        }

        internal sealed class StreamConsumerInfoProcessor : InterleavedStreamInfoProcessorBase<StreamConsumerInfo>
        {
            protected override StreamConsumerInfo ParseItem(RawResult result)
            {
                // Note: the base class passes a single consumer from the response into this method.

                // Response format:
                // > XINFO CONSUMERS mystream mygroup
                // 1) 1) name
                //    2) "Alice"
                //    3) pending
                //    4) (integer)1
                //    5) idle
                //    6) (integer)9104628
                // 2) 1) name
                //    2) "Bob"
                //    3) pending
                //    4) (integer)1
                //    5) idle
                //    6) (integer)83841983

                var arr = result.GetItems();

                return new StreamConsumerInfo(name: arr[1].AsRedisValue(),
                            pendingMessageCount: (int)arr[3].AsRedisValue(),
                            idleTimeInMilliseconds: (long)arr[5].AsRedisValue());
            }
        }

        internal sealed class StreamGroupInfoProcessor : InterleavedStreamInfoProcessorBase<StreamGroupInfo>
        {
            protected override StreamGroupInfo ParseItem(RawResult result)
            {
                // Note: the base class passes a single item from the response into this method.

                // Response format:
                // > XINFO GROUPS mystream
                // 1) 1) name
                //    2) "mygroup"
                //    3) consumers
                //    4) (integer)2
                //    5) pending
                //    6) (integer)2
                // 2) 1) name
                //    2) "some-other-group"
                //    3) consumers
                //    4) (integer)1
                //    5) pending
                //    6) (integer)0

                var arr = result.GetItems();

                return new StreamGroupInfo(name: arr[1].AsRedisValue(),
                    consumerCount: (int)arr[3].AsRedisValue(),
                    pendingMessageCount: (int)arr[5].AsRedisValue());
            }
        }

        internal abstract class InterleavedStreamInfoProcessorBase<T> : ResultProcessor<T[]>
        {
            protected abstract T ParseItem(RawResult result);

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.Type != ResultType.MultiBulk)
                {
                    return false;
                }

                var arr = result.GetItems();
                var parsedItems = Array.ConvertAll(arr, item => ParseItem(item));

                SetResult(message, parsedItems);
                return true;
            }
        }

        internal sealed class StreamInfoProcessor : StreamProcessorBase<StreamInfo>
        {
            // Parse the following format:
            // > XINFO mystream
            // 1) length
            // 2) (integer) 13
            // 3) radix-tree-keys
            // 4) (integer) 1
            // 5) radix-tree-nodes
            // 6) (integer) 2
            // 7) groups
            // 8) (integer) 2
            // 9) first-entry
            // 10) 1) 1524494395530-0
            //     2) 1) "a"
            //        2) "1"
            //        3) "b"
            //        4) "2"
            // 11) last-entry
            // 12) 1) 1526569544280-0
            //     2) 1) "message"
            //        2) "banana"
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.Type != ResultType.MultiBulk)
                {
                    return false;
                }

                var arr = result.GetItems();

                if (arr.Length != 12)
                {
                    return false;
                }

                // Note: Even if there is only 1 message in the stream, this command returns
                //       the single entry as the first-entry and last-entry in the response.

                // The first 8 items are interleaved name/value pairs.
                // Items 9-12 represent the first and last entry in the stream. The values will
                // be nil (stored in index 9 & 11) if the stream length is 0.

                var entries = ParseRedisStreamEntries(RawResult.CreateMultiBulk(arr[9], arr[11]));

                var streamInfo = new StreamInfo(length: (int)arr[1].AsRedisValue(),
                    radixTreeKeys: (int)arr[3].AsRedisValue(),
                    radixTreeNodes: (int)arr[5].AsRedisValue(),
                    groups: (int)arr[7].AsRedisValue(),
                    firstEntry: entries[0],
                    lastEntry: entries[1]);

                SetResult(message, streamInfo);
                return true;
            }
        }

        internal sealed class StreamPendingInfoProcessor : ResultProcessor<StreamPendingInfo>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                // Example:
                // > XPENDING mystream mygroup
                // 1) (integer)2
                // 2) 1526569498055 - 0
                // 3) 1526569506935 - 0
                // 4) 1) 1) "Bob"
                //       2) "2"
                // 5) 1) 1) "Joe"
                //       2) "8"

                if (result.Type != ResultType.MultiBulk)
                {
                    return false;
                }

                var arr = result.GetItems();

                if (arr.Length != 4)
                {
                    return false;
                }

                StreamConsumer[]? consumers = null;

                // If there are no consumers as of yet for the given group, the last
                // item in the response array will be null.
                if (!arr[3].IsNull)
                {
                    consumers = Array.ConvertAll(arr[3].GetItems(), item =>
                    {
                        var details = item.GetItems();

                        return new StreamConsumer(
                            name: details[0].AsRedisValue(),
                            pendingMessageCount: (int)details[1].AsRedisValue());
                    });
                }

                var pendingInfo = new StreamPendingInfo(pendingMessageCount: (int)arr[0].AsRedisValue(),
                    lowestId: arr[1].AsRedisValue(),
                    highestId: arr[2].AsRedisValue(),
                    consumers: consumers ?? new StreamConsumer[0]);
                    // ^^^^^
                    // Should we bother allocating an empty array only to prevent the need for a null check?

                SetResult(message, pendingInfo);
                return true;
            }
        }

        internal sealed class StreamPendingMessagesProcessor : ResultProcessor<StreamPendingMessageInfo[]>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                if (result.Type != ResultType.MultiBulk)
                {
                    return false;
                }

                var arr = result.GetItems();

                var messageInfoArray = Array.ConvertAll(arr, item =>
                {
                    var details = item.GetItems();

                    return new StreamPendingMessageInfo(messageId: details[0].AsRedisValue(),
                        consumerName: details[1].AsRedisValue(),
                        idleTimeInMs: (long)details[2].AsRedisValue(),
                        deliveryCount: (int)details[3].AsRedisValue());
                });

                SetResult(message, messageInfoArray);
                return true;
            }
        }

        internal abstract class StreamProcessorBase<T> : ResultProcessor<T>
        {
            // For command response formats see https://redis.io/topics/streams-intro.

            protected StreamEntry[] ParseRedisStreamEntries(RawResult result)
            {
                if (result.Type != ResultType.MultiBulk)
                {
                    return null;
                }

                var arr = result.GetItems();

                return Array.ConvertAll(arr, item =>
                {
                    if (item.IsNull || item.Type != ResultType.MultiBulk)
                    {
                        return StreamEntry.Null;
                    }

                    // Process the Multibulk array for each entry. The entry contains the following elements:
                    //  [0] = SimpleString (the ID of the stream entry)
                    //  [1] = Multibulk array of the name/value pairs of the stream entry's data
                    var entryDetails = item.GetItems();

                    return new StreamEntry(id: entryDetails[0].AsRedisValue(),
                        values: ParseStreamEntryValues(entryDetails[1]));
                });
            }

            protected NameValueEntry[] ParseStreamEntryValues(RawResult result)
            {
                // The XRANGE, XREVRANGE, XREAD commands return stream entries
                // in the following format.  The name/value pairs are interleaved
                // in the same fashion as the HGETALL response.
                //
                // 1) 1) 1518951480106-0
                //    2) 1) "sensor-id"
                //       2) "1234"
                //       3) "temperature"
                //       4) "19.8"
                // 2) 1) 1518951482479-0
                //    2) 1) "sensor-id"
                //       2) "9999"
                //       3) "temperature"
                //       4) "18.2"

                if (result.Type != ResultType.MultiBulk)
                {
                    return null;
                }

                var arr = result.GetItems();

                if (arr == null)
                {
                    return null;
                }

                // Calculate how many name/value pairs are in the stream entry.
                int count = arr.Length / 2;

                if (count == 0)
                {
                    return new NameValueEntry[0];
                }

                var pairs = new NameValueEntry[count];
                int offset = 0;

                for (int i = 0; i < pairs.Length; i++)
                {
                    pairs[i] = new NameValueEntry(arr[offset++].AsRedisValue(),
                                                  arr[offset++].AsRedisValue());
                }

                return pairs;
            }
        }

        private sealed class StringPairInterleavedProcessor : ValuePairInterleavedProcessorBase<KeyValuePair<string, string>>
        {
            protected override KeyValuePair<string, string> Parse(RawResult first, RawResult second)
            {
                return new KeyValuePair<string, string>(first.GetString(), second.GetString());
            }
        }

        private sealed class StringProcessor : ResultProcessor<string>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.Integer:
                    case ResultType.SimpleString:
                    case ResultType.BulkString:
                        SetResult(message, result.GetString());
                        return true;
                    case ResultType.MultiBulk:
                        var arr = result.GetItems();
                        if (arr.Length == 1)
                        {
                            SetResult(message, arr[0].GetString());
                            return true;
                        }
                        break;
                }
                return false;
            }
        }

        private class TracerProcessor : ResultProcessor<bool>
        {
            private static readonly byte[]
                authRequired = Encoding.UTF8.GetBytes("NOAUTH Authentication required."),
                authFail = Encoding.UTF8.GetBytes("ERR operation not permitted"),
                loading = Encoding.UTF8.GetBytes("LOADING ");

            private readonly bool establishConnection;

            public TracerProcessor(bool establishConnection)
            {
                this.establishConnection = establishConnection;
            }

            public override bool SetResult(PhysicalConnection connection, Message message, RawResult result)
            {
                var final = base.SetResult(connection, message, result);
                if (result.IsError)
                {
                    if (result.IsEqual(authFail) || result.IsEqual(authRequired))
                    {
                        connection.RecordConnectionFailed(ConnectionFailureType.AuthenticationFailure, new Exception(result.ToString() + " Verify if the Redis password provided is correct."));
                    }
                    else if (result.AssertStarts(loading))
                    {
                        connection.RecordConnectionFailed(ConnectionFailureType.Loading);
                    }
                    else
                    {
                        connection.RecordConnectionFailed(ConnectionFailureType.ProtocolFailure);
                    }
                }
                return final;
            }

            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                bool happy;
                switch (message.Command)
                {
                    case RedisCommand.ECHO:
                        happy = result.Type == ResultType.BulkString && (!establishConnection || result.IsEqual(connection.Multiplexer.UniqueId));
                        break;
                    case RedisCommand.PING:
                        happy = result.Type == ResultType.SimpleString && result.IsEqual(RedisLiterals.BytesPONG);
                        break;
                    case RedisCommand.TIME:
                        happy = result.Type == ResultType.MultiBulk && result.GetItems().Length == 2;
                        break;
                    case RedisCommand.EXISTS:
                        happy = result.Type == ResultType.Integer;
                        break;
                    default:
                        happy = true;
                        break;
                }
                if (happy)
                {
                    if (establishConnection) connection.Bridge.OnFullyEstablished(connection);
                    SetResult(message, happy);
                    return true;
                }
                else
                {
                    connection.RecordConnectionFailed(ConnectionFailureType.ProtocolFailure);
                    return false;
                }
            }
        }

        #region Sentinel

        private sealed class SentinelGetMasterAddressByNameProcessor : ResultProcessor<EndPoint>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var arr = result.GetItemsAsValues();

                        int port;
                        if (arr.Length == 2 && int.TryParse(arr[1], out port))
                        {
                            SetResult(message, Format.ParseEndPoint(arr[0], port));
                            return true;
                        }
                        else if (arr.Length == 0)
                        {
                            SetResult(message, null);
                            return true;
                        }
                        break;
                    case ResultType.SimpleString:
                        //We don't want to blow up if the master is not found
                        if (result.IsNull)
                            return true;
                        break;
                }
                return false;
            }
        }

        private sealed class SentinelArrayOfArraysProcessor : ResultProcessor<KeyValuePair<string, string>[][]>
        {
            protected override bool SetResultCore(PhysicalConnection connection, Message message, RawResult result)
            {
                var innerProcessor = StringPairInterleaved as StringPairInterleavedProcessor;
                if (innerProcessor == null)
                {
                    return false;
                }

                switch (result.Type)
                {
                    case ResultType.MultiBulk:
                        var arrayOfArrays = result.GetArrayOfRawResults();

                        var returnArray = new KeyValuePair<string, string>[arrayOfArrays.Length][];

                        for (int i = 0; i < arrayOfArrays.Length; i++)
                        {
                            var rawInnerArray = arrayOfArrays[i];
                            innerProcessor.TryParse(rawInnerArray, out KeyValuePair<string, string>[] kvpArray);
                            returnArray[i] = kvpArray;
                        }

                        SetResult(message, returnArray);
                        return true;
                }
                return false;
            }
        }

        #endregion
    }

    internal abstract class ResultProcessor<T> : ResultProcessor
    {
        protected void SetResult(Message message, T value)
        {
            if (message == null) return;
            var box = message.ResultBox as ResultBox<T>;
            message.SetResponseReceived();

            box?.SetResult(value);
        }
    }
}
