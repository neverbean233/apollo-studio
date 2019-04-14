using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

using Newtonsoft.Json;

using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Devices {
    public class Fade: Device {
        public static readonly new string DeviceIdentifier = "fade";

        private int _time; // milliseconds
        public List<Color> Colors = new List<Color>();
        public List<Decimal> Positions = new List<Decimal>();
        private List<Color> _steps = new List<Color>();
        private List<int> _counts = new List<int>();
        private List<int> _cutoffs = new List<int>();
        
        private int[] _indexes = new int[128];
        private object[] locker = new object[128];

        private List<Timer>[] _timers = new List<Timer>[128];
        private TimerCallback _timerexit;

        public int Time {
            get => _time;
            set {
                if (0 <= value) _time = value;
            }
        }

        private void Generate() {
            _steps = new List<Color>();
            _counts = new List<int>();
            _cutoffs = new List<int>() {0};

            for (int i = 0; i < Colors.Count - 1; i++) {
                _counts.Add(new int[] {
                    Math.Abs(Colors[i].Red - Colors[i + 1].Red),
                    Math.Abs(Colors[i].Green - Colors[i + 1].Green),
                    Math.Abs(Colors[i].Blue - Colors[i + 1].Blue)
                }.Max());

                for (int j = 0; j < _counts.Last(); j++) {
                    _steps.Add(new Color(
                        (byte)(Colors[i].Red + (Colors[i + 1].Red - Colors[i].Red) * j / _counts.Last()),
                        (byte)(Colors[i].Green + (Colors[i + 1].Green - Colors[i].Green) * j / _counts.Last()),
                        (byte)(Colors[i].Blue + (Colors[i + 1].Blue - Colors[i].Blue) * j / _counts.Last())
                    ));
                }

                if (i > 0) {
                    _cutoffs.Add(_counts.Last() + _cutoffs.Last());
                } else {
                    _cutoffs.Add(_counts.Last());
                }
            }

            _steps.Add(Colors.Last());

            if (_steps.Last().Lit) {
                _steps.Add(new Color(0));
                _cutoffs[_cutoffs.Count - 1]++;
            }
        }

        public Color this[int index] {
            get => Colors[index];
        }

        public int Count {
            get => Colors.Count;
        }

        public override Device Clone() => new Fade(_time, Colors, Positions);

        public void Insert(int index, Color color, Decimal position) {
            Colors.Insert(index, color);
            Positions.Insert(index, position);
            Generate();
        }

        public void Remove(int index) {
            Colors.RemoveAt(index);
            Positions.RemoveAt(index);
            Generate();
        }

        public Fade(int time = 1000, List<Color> colors = null, List<Decimal> positions = null): base(DeviceIdentifier) {
            _timerexit = new TimerCallback(Tick);

            if (colors == null) colors = new List<Color>() {new Color(63), new Color(0)};
            if (positions == null) positions = new List<Decimal>() {0, 1};
            
            Time = time;
            Colors = colors;
            Positions = positions;

            for (int i = 0; i < 128; i++)
                locker[i] = new object();

            Generate();
        }

        private void Tick(object info) {
            if (info.GetType() == typeof((byte, int))) {
                (byte index, int layer) = ((byte, int))info;

                lock (locker[index]) {
                    int color = ++_indexes[index];

                    if (color < _steps.Count)
                        MIDIExit?.Invoke(new Signal(Track.Get(this).Launchpad, index, _steps[color].Clone(), layer));
                }
            }
        }

        public override void MIDIEnter(Signal n) {
            if (Colors.Count > 0 && n.Color.Lit) {
                if (_timers[n.Index] != null) 
                    for (int i = 0; i < _timers[n.Index].Count; i++) 
                        _timers[n.Index][i].Dispose();

                _timers[n.Index] = new List<Timer>();
                _indexes[n.Index] = 0;

                n.Color = _steps[0].Clone();

                MIDIExit?.Invoke(n);

                int j = 0;
                for (int i = 1; i < _steps.Count; i++) {
                    if (_cutoffs[j + 1] == i) j++;

                    if (j < Colors.Count - 1)
                        _timers[n.Index].Add(new Timer(
                            _timerexit,
                            (n.Index, n.Layer),
                            (int)((Positions[j] + (Positions[j + 1] - Positions[j]) * (i - _cutoffs[j]) / _counts[j]) * _time),
                            Timeout.Infinite
                        ));
                }

                _timers[n.Index].Add(new Timer(_timerexit, (n.Index, n.Layer), _time, Timeout.Infinite));
            }
        }

        public static Device DecodeSpecific(string jsonString) {
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            if (json["device"].ToString() != DeviceIdentifier) return null;

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json["data"].ToString());
            
            List<object> colors = JsonConvert.DeserializeObject<List<object>>(data["colors"].ToString());
            List<Color> initC = new List<Color>();

            foreach (object color in colors)
                initC.Add(Color.Decode(color.ToString()));

            List<object> positions = JsonConvert.DeserializeObject<List<object>>(data["positions"].ToString());
            List<Decimal> initP = new List<Decimal>();

            foreach (object position in positions)
                initP.Add(Decimal.Parse(position.ToString()));

            return new Fade(
                Convert.ToInt32(data["time"]),
                initC,
                initP
            );
        }

        public override string EncodeSpecific() {
            StringBuilder json = new StringBuilder();

            using (JsonWriter writer = new JsonTextWriter(new StringWriter(json))) {
                writer.WriteStartObject();

                    writer.WritePropertyName("device");
                    writer.WriteValue(DeviceIdentifier);

                    writer.WritePropertyName("data");
                    writer.WriteStartObject();

                        writer.WritePropertyName("time");
                        writer.WriteValue(_time);

                        writer.WritePropertyName("colors");

                        writer.WriteStartArray();

                            for (int i = 0; i < Colors.Count; i++)
                                writer.WriteRawValue(Colors[i].Encode());

                        writer.WriteEndArray();

                        writer.WritePropertyName("positions");
                        writer.WriteStartArray();

                            for (int i = 0; i < Positions.Count; i++)
                                writer.WriteValue(Positions[i]);

                        writer.WriteEndArray();

                    writer.WriteEndObject();

                writer.WriteEndObject();
            }
            
            return json.ToString();
        }
    }
}