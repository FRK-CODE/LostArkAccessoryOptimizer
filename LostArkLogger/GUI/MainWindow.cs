﻿using AccessoryOptimizer.Models;
using AccessoryOptimizer.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static AccessoryOptimizer.Services.PermutationService;

namespace LostArkLogger
{
    public partial class MainWindow : Form, INotifyPropertyChanged
    {
        private PermutationService _permutationService;

        Parser sniffer;
        Overlay overlay;
        private int _packetCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string PacketCount
        {
            get { return "Logged Packets: " + _packetCount; }
        }

        public MainWindow()
        {
            InitializeComponent();
            //versionLabel.Text = "v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            Oodle.Init();
            if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
            sniffer = new Parser();
            sniffer.onPacketTotalCount += (int totalPacketCount) =>
            {
                _packetCount = totalPacketCount;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PacketCount)));
            };
            loggedPacketCountLabel.Text = "Logged Packets : 0";
            loggedPacketCountLabel.DataBindings.Add("Text", this, nameof(PacketCount));

            overlay = new Overlay();
            overlay.AddSniffer(sniffer);
            // overlay.Show();

            _permutationService = new PermutationService();
            InitializeOptions();
        }

        #region Other Stuff
        private void sniffModeCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            this.sniffModeCheckbox.Enabled = false;
            this.sniffer.use_npcap = sniffModeCheckbox.Checked;
            this.sniffer.InstallListener();
            // This will unset the checkbox if it fails to initialize
            this.sniffModeCheckbox.Checked = this.sniffer.use_npcap;
            this.sniffModeCheckbox.Enabled = true;
            Properties.Settings.Default.Npcap = sniffModeCheckbox.Checked;
            Properties.Settings.Default.Save();
        }
        #endregion

        #region My Stuff
        private void clearButton_Click(object sender, EventArgs e)
        {
            ClearAccessories();
            UpdateCountText();
        }

        private void processButton_Click(object sender, EventArgs e)
        {
            message_Label.Text = "Processing now!";
            string message = string.Empty;
            var allDesiredEngravings = GetDesiredEngravings();

            if (string.IsNullOrEmpty(desiredStatType1.SelectedItem.ToString()))
            {
                message = "Desired stat type 1 needs to be set";
            }
            else
            {
                PSO.DesiredStatType1 = Enum.Parse<Stat_Type>(desiredStatType1.SelectedItem.ToString());
            }

            if (string.IsNullOrEmpty(desiredStatType2.SelectedItem.ToString()))
            {
                message = "Desired stat type 2 needs to be set";
            }
            else
            {
                PSO.DesiredStatType2 = Enum.Parse<Stat_Type>(desiredStatType2.SelectedItem.ToString());
            }

            if (!string.IsNullOrEmpty(message))
            {
                message_Label.Text = message;
                return;
            }

            _permutationService._necklaces = PSO.CurrentAccessories.Where(a => a.AccessoryType == AccessoryType.Necklace).ToList();
            _permutationService._earrings = PSO.CurrentAccessories.Where(a => a.AccessoryType == AccessoryType.Earring).ToList();
            _permutationService._rings = PSO.CurrentAccessories.Where(a => a.AccessoryType == AccessoryType.Ring).ToList();

            if (PSO.DesiredStatType1 != null && PSO.DesiredStatType2 != null)
            {
                _permutationService._necklaces = _permutationService._necklaces.Where(n => (n.Stats.StatType1 == PSO.DesiredStatType1 || n.Stats.StatType2 == PSO.DesiredStatType1) && (n.Stats.StatType1 == PSO.DesiredStatType2 || n.Stats.StatType2 == PSO.DesiredStatType2)).ToList();
            }

            //_permutationService._necklaces.Add(new Accessory(AccessoryType.Necklace, AccessoryRank.Relic, 92, 0, 0, new() { new Engraving(EngravingType.Keen_Blunt_Weapon, 5), new Engraving(EngravingType.Hit_Master, 3) }, new Engraving(EngravingType.Atk_Power_Reduction, 2), new Stats(Stat_Type.Crit, 484, Stat_Type.Specialization, 500)));
            //_permutationService._rings.Add(new Accessory(AccessoryType.Ring, AccessoryRank.Legendary, 98, 0, 0, new() { new Engraving(EngravingType.Demonic_Impulse, 3), new Engraving(EngravingType.Grudge, 3) }, new Engraving(EngravingType.Defence_Reduction, 2), new Stats(Stat_Type.Specialization, 179)));
            //_permutationService._rings.Add(new Accessory(AccessoryType.Ring, AccessoryRank.Relic, 100, 0, 0, new() { new Engraving(EngravingType.Adrenaline, 5), new Engraving(EngravingType.Hit_Master, 3) }, new Engraving(EngravingType.Move_Speed_Reduction, 3), new Stats(Stat_Type.Specialization, 200)));

            List<PermutationDisplay> permutations = _permutationService.Process(allDesiredEngravings, int.Parse(maxCost.Text), reuse_checkBox.Checked, filterWorryingNeg_checkBox.Checked, filterZeroNegEngraving_checkBox.Checked);

            message_Label.Text = $"Total Results ({permutations.Count})";

            if (MinimumStatHasValue(min_stat1, out int amount1))
            {
                permutations = FilterByMinStat(permutations, (Stat_Type)PSO.DesiredStatType1, amount1);
            }

            if (MinimumStatHasValue(min_stat2, out int amount2))
            {
                permutations = FilterByMinStat(permutations, (Stat_Type)PSO.DesiredStatType2, amount2);
            }

            message_Label.Text += $" - Filtered Results ({permutations.Count})";

            cheapest_Textbox.Text = GetStringOutputOfResults(permutations
                .OrderBy(p => p.Cost)
                .ToList());

            cheapest500HighStat1_textBox.Text = GetStringOutputOfResults(permutations
                .OrderBy(p => p.Cost)
                .Take(500)
                .OrderBy(p => PSO.DesiredStatType1 == Stat_Type.Crit ? p.StatsValue.CritValue : (PSO.DesiredStatType1 == Stat_Type.Specialization ? p.StatsValue.SpecValue : p.StatsValue.SwiftValue))
                .Reverse()
                .Take(5)
                .ToList());

            cheapest500HighStat2_textBox.Text = GetStringOutputOfResults(permutations
                .OrderBy(p => p.Cost)
                .Take(500)
                .OrderBy(p => PSO.DesiredStatType2 == Stat_Type.Crit ? p.StatsValue.CritValue : (PSO.DesiredStatType2 == Stat_Type.Specialization ? p.StatsValue.SpecValue : p.StatsValue.SwiftValue))
                .Reverse()
                .Take(5)
                .ToList());

            cheapest80Q_textBox.Text = GetStringOutputOfResults(permutations
                .Where(p => p.AverageQuality >= 80)
                .OrderBy(p => p.Cost)
                .Take(5)
                .ToList());

            cheapest90Q_textBox.Text = GetStringOutputOfResults(permutations
                .Where(p => p.AverageQuality >= 90)
                .OrderBy(p => p.Cost)
                .Take(5)
                .ToList());

            cheapest95Q_textBox.Text = GetStringOutputOfResults(permutations
                .Where(p => p.AverageQuality >= 95)
                .OrderBy(p => p.Cost)
                .Take(5)
                .ToList());

            cheapestWithRelicNeck_textBox.Text = GetStringOutputOfResults(permutations
                .Where(p => p.Necklace.AccessoryRank == AccessoryRank.Relic)
                .OrderBy(p => p.Cost)
                .Take(5)
                .ToList());

            highestStat1_textBox.Text = GetStringOutputOfResults(permutations
                .OrderBy(p => PSO.DesiredStatType1 == Stat_Type.Crit ? p.StatsValue.CritValue : (PSO.DesiredStatType1 == Stat_Type.Specialization ? p.StatsValue.SpecValue : p.StatsValue.SwiftValue))
                .Reverse()
                .Take(5)
                .ToList());

            highestStat2_textBox.Text = GetStringOutputOfResults(permutations
                .OrderBy(p => PSO.DesiredStatType2 == Stat_Type.Crit ? p.StatsValue.CritValue : (PSO.DesiredStatType2 == Stat_Type.Specialization ? p.StatsValue.SpecValue : p.StatsValue.SwiftValue))
                .Reverse()
                .Take(5)
                .ToList());
        }

        private List<PermutationDisplay> FilterByMinStat(List<PermutationDisplay> permutationDisplays, Stat_Type statType, int minAmount)
        {
            switch (statType)
            {
                case Stat_Type.Crit:
                    return permutationDisplays.Where(p => p.StatsValue.CritValue >= minAmount).ToList();
                case Stat_Type.Specialization:
                    return permutationDisplays.Where(p => p.StatsValue.SpecValue >= minAmount).ToList();
                case Stat_Type.Swiftness:
                    return permutationDisplays.Where(p => p.StatsValue.SwiftValue >= minAmount).ToList();
            }

            return permutationDisplays;
        }

        private bool MinimumStatHasValue(TextBox textBox, out int amount)
        {
            amount = 0;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                return false;
            }

            if (!int.TryParse(textBox.Text, out int result))
            {
                return false;
            }

            if (result > 0)
            {
                amount = result;
                return true;
            }

            return false;
        }

        private void InitializeOptions()
        {
            engraving1Choice.DataSource = GetChoices<EngravingType>();
            engraving2Choice.DataSource = GetChoices<EngravingType>();
            engraving3Choice.DataSource = GetChoices<EngravingType>();
            engraving4Choice.DataSource = GetChoices<EngravingType>();
            engraving5Choice.DataSource = GetChoices<EngravingType>();
            engraving6Choice.DataSource = GetChoices<EngravingType>();

            desiredStatType1.DataSource = GetChoices<Stat_Type>();
            desiredStatType2.DataSource = GetChoices<Stat_Type>();
        }

        private string[] GetChoices<T>()
        {
            string[] choices = new[] { string.Empty };
            choices = choices.Concat(Enum.GetNames(typeof(T))).OrderBy(c => c).ToArray();
            return choices;
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            UpdateCountText();
        }

        public void UpdateCountText()
        {
            accessoryCount.Text = PSO.CurrentAccessories.Count.ToString();
            earringCount.Text = PSO.CurrentAccessories.Where(ca => ca.AccessoryType == AccessoryType.Earring).Count().ToString();
            ringCount.Text = PSO.CurrentAccessories.Where(ca => ca.AccessoryType == AccessoryType.Ring).Count().ToString();
            necklaceCount.Text = PSO.CurrentAccessories.Where(ca => ca.AccessoryType == AccessoryType.Necklace).Count().ToString();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            StoreAccessories();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            LoadAccessories();
            UpdateCountText();
        }

        private List<List<DesiredEngraving>> GetDesiredEngravings()
        {
            List<List<DesiredEngraving>> overallDesiredEngravings = new();

            for (int i = 1; i < 7; i++)
            {
                string firstEngravingQuantity = Controls[$"engraving1Quantity_{i}"].Text;

                if (!string.IsNullOrEmpty(firstEngravingQuantity) && firstEngravingQuantity != "0")
                {
                    overallDesiredEngravings.Add(GetDesiredEngraving(i));
                }
            }

            return overallDesiredEngravings;
        }

        private List<DesiredEngraving> GetDesiredEngraving(int columnIndex)
        {
            List<DesiredEngraving> desiredEngravings = new();

            for (int i = 1; i < 7; i++)
            {
                AddEngravingIfSet(desiredEngravings, Controls[$"engraving{i}Choice"] as ComboBox, Controls[$"engraving{i}Quantity_{columnIndex}"] as TextBox);
            }

            return desiredEngravings;
        }

        private string GetStringOutputOfResults(List<PermutationDisplay> permutations)
        {
            StringBuilder overallString = new();

            foreach (PermutationDisplay p in permutations)
            {
                overallString.AppendLine("------------------------------------------------------");

                overallString.AppendLine("\n");
                overallString.AppendLine($"Cost: {p.Cost}       AverageQuality: {p.AverageQuality}       Cost per Quality: {p.Cost / p.AverageQuality}");
                overallString.AppendLine($"Crit: {p.StatsValue.CritValue}       Swiftness: {p.StatsValue.SwiftValue}           Specialization: {p.StatsValue.SpecValue}");
                overallString.AppendLine($"Atk Pow: -{p.NegativeSummary.AmountOfAtkPower}       Atk Spd: -{p.NegativeSummary.AmountOfAtkSpeed}       Mov: -{p.NegativeSummary.AmountOfMoveSpeed}       Def: -{p.NegativeSummary.AmountOfDefenceReduction}");
                overallString.AppendLine("\n");

                foreach (var a in p.GetAccessories())
                {
                    string output = string.Format("{0,-30} {1,-60} {2,-60} {3,-60} {4,-15} {5,-15}", $"{a.AccessoryType} {a.Quality}", $"{a.Engravings[0].EngravingType} ({a.Engravings[0].EngravingValue})", $"{a.Engravings[1].EngravingType}({a.Engravings[1].EngravingValue})", $"{a.NegativeEngraving.EngravingType} ({a.NegativeEngraving.EngravingValue})", $"Bid: {a.MinimumBid}", $"Cost: {a.BuyNowPrice}");
                    overallString.AppendLine(output);
                }

                overallString.AppendLine("\n");
            }

            return overallString.ToString();
        }

        private static void AddEngravingIfSet(List<DesiredEngraving> desiredEngravings, ComboBox choiceComboBox, TextBox quantityTextBox)
        {
            if (!string.IsNullOrEmpty(choiceComboBox.Text))
            {
                if (!string.IsNullOrEmpty(quantityTextBox.Text) && quantityTextBox.Text != "0")
                {
                    desiredEngravings.Add(new DesiredEngraving(int.Parse(quantityTextBox.Text), Enum.Parse<EngravingType>(choiceComboBox.Text)));
                }
            }
        }

        private void saveLastEngravingsButton_Click(object sender, EventArgs e)
        {
            List<List<DesiredEngraving>> allDesiredEngravings = GetDesiredEngravings();

            string json = JsonSerializer.Serialize(allDesiredEngravings, new JsonSerializerOptions() { IncludeFields = true });
            File.WriteAllText(@".\savedEngravings.json", json);
        }

        private void loadLastEngravingsButton_Click(object sender, EventArgs e)
        {
            try
            {
                string json = File.ReadAllText(@".\savedEngravings.json");
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }
                else
                {
                    List<List<DesiredEngraving>> allDesiredEngravings = JsonSerializer.Deserialize<List<List<DesiredEngraving>>>(json);

                    foreach (var (desiredEngravings, k) in allDesiredEngravings.Select((value, k) => (value, k)))
                    {
                        foreach (var (desiredEngraving, j) in desiredEngravings.Select((value, j) => (value, j)))
                        {
                            ComboBox comboBox = ((ComboBox)Controls[$"engraving{j + 1}Choice"]);
                            comboBox.SelectedIndex = comboBox.FindString(desiredEngraving.EngravingType.ToString());
                            Controls[$"engraving{j + 1}Quantity_{k + 1}"].Text = desiredEngraving.Amount.ToString();
                        }
                    }

                }
            }
            catch
            {


            }
        }

        private void ClearAccessories()
        {
            PSO.CurrentAccessories = new();
        }

        private void StoreAccessories()
        {
            string json = JsonSerializer.Serialize(PSO.CurrentAccessories, new JsonSerializerOptions() { IncludeFields = true });
            File.WriteAllText(@".\data.json", json);
        }

        private bool LoadAccessories()
        {
            try
            {
                string json = File.ReadAllText(@".\data.json");
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }
                else
                {
                    PSO.CurrentAccessories = JsonSerializer.Deserialize<List<Accessory>>(json);
                    return PSO.CurrentAccessories?.Count() > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
