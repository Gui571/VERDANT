using Godot;
namespace Verdant;
public partial class Workshop : Control
{
    Genome current = Genome.Generate(1042, "beast"), saved = Genome.Generate(1042, "beast"), parentB = Genome.Generate(1043, "beast");
    readonly History history = new();
    WorldView view = null!;
    Label status = null!, title = null!;
    LineEdit seed = null!;
    OptionButton family = null!;
    VBoxContainer inspector = null!;
    HBoxContainer children = null!;
    bool syncing, lockAnatomy, lockColor, lockPattern;
    float mutation = .2f;
    readonly List<ImageTexture> thumbnails = new();
    static readonly Color Muted = new("8baba2"), Text = new("e6eee4"), Gold = new("edc779");
    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs(); if (args.Contains("--self-test")) { GetTree().Quit(SelfTests.Run()); return; }
        Theme = MakeTheme(); if (args.Contains("--gallery-capture")) { AddChild(new GalleryCapture()); return; }
        Build(); Apply(current, false);
        if (args.Contains("--ui-test")) { RunUiTests(); }
        if (args.Contains("--capture")) { CallDeferred(MethodName.Capture); }
    }
    async void RunUiTests()
    {
        int checks = 0; void Verify(bool value, string what) { if (!value) throw new Exception(what); checks++; }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        try
        {
            var initial = current; Save(); Apply(current.Mutate(211, .5f)); Load(); Verify(current == initial, "save/load");
            lockAnatomy = lockColor = lockPattern = true; Generate(777); Verify(current.Mass == initial.Mass && current.Hue == initial.Hue && current.Pattern == initial.Pattern, "feature locks"); lockAnatomy = lockColor = lockPattern = false;
            foreach (var plan in Plans.All) { Apply(Genome.Generate(800, plan.Id)); Verify(view.Genome == current, "preview genome"); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            var before = current; history.Push(before); sliders[0].slider.Value = .6; Verify(current.Mass == .6f, "slider edits"); Apply(history.Undo(current), false); Verify(current == before, "UI undo"); Apply(history.Redo(current), false); Verify(current.Mass == .6f, "UI redo");
            view.Paused = true; double t = view.Pose.Time; view.Step(); Verify(view.Pose.Time > t, "frame step"); view.SetGenome(initial); view.FieldMode = true; view.Paused = false;
            for (int i = 0; i < 20; i++) { Apply(Genome.Generate((uint)i, "beast")); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            Exporter.Png(current, view.Pose, UserPath("creature.png")); Verify(File.Exists(UserPath("creature.png")), "PNG output");
            Exporter.Sheet(current, UserPath("spritesheet.png"), MotionState.Walk, 0); Verify(File.Exists(UserPath("spritesheet.png.json")), "sheet metadata");
            for (int i = 0; i < 20; i++) { var actor = new CreatureActor(); AddChild(actor); actor.SetGenome(Genome.Generate((uint)i, "plant")); actor.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            GD.Print("UI_TEST_PASS checks=" + checks + "; 20 rapid genome edits; 20 actor lifecycles"); GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("UI_TEST_FAIL " + e); GetTree().Quit(1); }
    }
    async void Capture() { await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout); using var image = GetViewport().GetTexture().GetImage(); image.SavePng("user://workshop.png"); GD.Print("Capture: " + ProjectSettings.GlobalizePath("user://workshop.png")); GetTree().Quit(); }
    Theme MakeTheme()
    {
        var theme = new Theme(); theme.DefaultFontSize = 15; theme.SetColor("font_color", "Label", Text); theme.SetColor("font_color", "Button", Text); theme.SetColor("font_color", "CheckBox", Muted);
        foreach (var type in new[] { "Button", "OptionButton", "LineEdit", "SpinBox" }) { var box = new StyleBoxFlat { BgColor = new Color("203730"), BorderColor = new Color("365147"), BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1, CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5, ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 9, ContentMarginBottom = 9 }; theme.SetStylebox("normal", type, box); var hover = (StyleBoxFlat)box.Duplicate(); hover.BgColor = new Color("365447"); theme.SetStylebox("hover", type, hover); theme.SetStylebox("pressed", type, hover); theme.SetStylebox("focus", type, new StyleBoxEmpty()); }
        theme.SetConstant("separation", "VBoxContainer", 10); theme.SetConstant("separation", "HBoxContainer", 10); return theme;
    }
    Label Label(string text, int size = 15, Color? color = null) { var l = new Label { Text = text }; l.AddThemeFontSizeOverride("font_size", size); if (color.HasValue) l.AddThemeColorOverride("font_color", color.Value); return l; }
    Button Button(string text, Action action, Container parent) { var b = new Button { Text = text, MouseDefaultCursorShape = CursorShape.PointingHand }; b.Pressed += () => Safe(action); parent.AddChild(b); return b; }
    void Safe(Action action) { try { action(); } catch (Exception e) { status.Text = e.Message; GD.PushWarning(e.Message); } }
    void Build()
    {
        var bg = new ColorRect { Color = new Color("0e1b19"), MouseFilter = MouseFilterEnum.Ignore }; bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(bg); var margin = new MarginContainer(); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 24); AddChild(margin); var all = new VBoxContainer(); margin.AddChild(all);
        var top = new HBoxContainer(); all.AddChild(top); var brand = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; top.AddChild(brand); brand.AddChild(Label("V E R D A N T", 27, Gold)); brand.AddChild(Label("PROCEDURAL CREATURE WORKSHOP   /   FIELD NOTES 001", 12, Muted)); Button("Save genome", Save, top); Button("Load genome", Load, top); Button("Export PNG", () => { Exporter.Png(current, view.Pose, UserPath("creature.png")); Notify("PNG exported to " + UserPath("creature.png")); }, top); Button("Sprite sheet ↗", () => { Exporter.Sheet(current, UserPath("spritesheet.png"), view.Pose.State, view.Pose.Direction); Notify("16 frames + metadata exported to user data folder."); }, top);
        all.AddChild(new HSeparator()); var content = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; all.AddChild(content);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(270, 0), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; content.AddChild(scroll); inspector = new VBoxContainer { CustomMinimumSize = new Vector2(250, 0) }; scroll.AddChild(inspector); inspector.AddChild(Label("01   SPECIMEN", 12, Gold)); family = new OptionButton(); foreach (var p in Plans.All) family.AddItem(p.Label); family.ItemSelected += i => { if (!syncing) Apply(Genome.Generate(current.Seed, Plans.All[(int)i].Id)); }; inspector.AddChild(family); var seedRow = new HBoxContainer(); inspector.AddChild(seedRow); seed = new LineEdit { PlaceholderText = "Seed", SizeFlagsHorizontal = SizeFlags.ExpandFill }; seedRow.AddChild(seed); Button("Generate", () => { if (!uint.TryParse(seed.Text, out var s)) throw new ArgumentException("Enter a seed between 0 and 4294967295."); Generate(s); }, seedRow); Button("✦   New creature", () => Generate(current.Seed + 1), inspector);
        inspector.AddChild(Label("02   ANATOMY", 12, Gold)); Slider("Body mass", .55, 1.5, g => g.Mass, (g, v) => g with { Mass = v }); Slider("Body length", .55, 1.5, g => g.Length, (g, v) => g with { Length = v }); Slider("Leg length", .55, 1.5, g => g.Legs, (g, v) => g with { Legs = v }); Slider("Tail length", .55, 1.5, g => g.Tail, (g, v) => g with { Tail = v }); Slider("Head scale", .55, 1.5, g => g.Head, (g, v) => g with { Head = v }); Slider("Crown / horns", 0, 1, g => g.Horns, (g, v) => g with { Horns = v }); Check("Lock anatomy", v => lockAnatomy = v, inspector);
        inspector.AddChild(Label("03   MATERIAL & MOVEMENT", 12, Gold)); Slider("Palette hue", 0, 1, g => g.Hue, (g, v) => g with { Hue = v }); Slider("Markings", 0, 1, g => g.Pattern, (g, v) => g with { Pattern = v }); Slider("Gait tempo", .55, 1.5, g => g.Tempo, (g, v) => g with { Tempo = v }); Check("Lock palette", v => lockColor = v, inspector); Check("Lock markings", v => lockPattern = v, inspector); var hist = new HBoxContainer(); inspector.AddChild(hist); Button("Undo", () => Apply(history.Undo(current), false), hist); Button("Redo", () => Apply(history.Redo(current), false), hist); Button("Reset", () => Apply(saved), hist);
        var main = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; content.AddChild(main); var header = new HBoxContainer(); main.AddChild(header); title = Label("", 24); title.SizeFlagsHorizontal = SizeFlags.ExpandFill; header.AddChild(title); var mode = new OptionButton(); mode.AddItem("Workshop"); mode.AddItem("Field test · WASD"); mode.ItemSelected += i => { view.FieldMode = i == 1; view.QueueRedraw(); }; header.AddChild(mode);
        view = new WorldView { CustomMinimumSize = new Vector2(550, 340), SizeFlagsVertical = SizeFlags.ExpandFill }; main.AddChild(view);
        var controls = new HBoxContainer(); main.AddChild(controls); var state = new OptionButton(); foreach (var s in Enum.GetNames<MotionState>()) state.AddItem(s); state.ItemSelected += i => view.Pose.SetState((MotionState)i); controls.AddChild(state); var direction = new OptionButton(); foreach (var d in new[] { "East", "West", "North", "South" }) direction.AddItem(d); direction.ItemSelected += i => view.Pose.Direction = (int)i; controls.AddChild(direction); Button("Pause / play", () => view.Paused = !view.Paused, controls); Button("Step", () => { view.Paused = true; view.Step(); }, controls); Button("½ speed", () => view.Pose.Speed = view.Pose.Speed == 1 ? .5f : 1, controls); Button("Zoom", () => { view.Zoom = view.Zoom == 4 ? 1 : view.Zoom + 1; view.QueueRedraw(); }, controls); Check("Landscape", v => { view.Landscape = v; view.QueueRedraw(); }, controls, true); Check("Rig", v => view.Rig = v, controls);
        var offspringHeader = new HBoxContainer(); main.AddChild(offspringHeader); offspringHeader.AddChild(Label("04   LINEAGE", 12, Gold)); offspringHeader.AddChild(Label("Select a child to adopt", 12, Muted)); Button("Set parent B", () => { parentB = current; Notify("Parent B captured: " + current.Seed); }, offspringHeader); Button("Cross A × B", () => Apply(Genome.Cross(current, parentB, current.Seed + 17)), offspringHeader); var strength = new HSlider { MinValue = 0, MaxValue = 1, Step = .05, Value = .2, CustomMinimumSize = new Vector2(100, 0) }; strength.ValueChanged += v => { mutation = (float)v; RefreshChildren(); }; offspringHeader.AddChild(strength); children = new HBoxContainer { CustomMinimumSize = new Vector2(0, 145) }; main.AddChild(children);
        main.AddChild(Label("05   FIELD GUIDE     /     54 REPRODUCIBLE SPECIMENS", 12, Gold)); var gallery = new HBoxContainer(); main.AddChild(gallery); foreach (var p in Plans.All) { var b = Button(p.Label, () => Apply(Genome.Generate(600 + ((current.Seed + 1) % 6), p.Id)), gallery); b.AddThemeFontSizeOverride("font_size", 12); b.SizeFlagsHorizontal = SizeFlags.ExpandFill; }
        status = Label("Deterministic by nature. Every shape, every step, grown from a seed.", 12, Muted); all.AddChild(new HSeparator()); all.AddChild(status);
    }
    readonly List<(HSlider slider, Func<Genome, float> get)> sliders = new();
    void Slider(string label, double min, double max, Func<Genome, float> get, Func<Genome, float, Genome> set) { var row = new HBoxContainer(); inspector.AddChild(row); row.AddChild(Label(label, 13, Muted)); var value = Label("", 12, Gold); value.SizeFlagsHorizontal = SizeFlags.ExpandFill; value.HorizontalAlignment = HorizontalAlignment.Right; row.AddChild(value); var slider = new HSlider { MinValue = min, MaxValue = max, Step = .01 }; inspector.AddChild(slider); slider.DragStarted += () => history.Push(current); slider.ValueChanged += v => { value.Text = v.ToString("0.00"); if (!syncing) Apply(set(current, (float)v), false); }; sliders.Add((slider, get)); }
    void Check(string label, Action<bool> changed, Container parent, bool initial = false) { var check = new CheckBox { Text = label, ButtonPressed = initial }; check.Toggled += v => changed(v); parent.AddChild(check); }
    void Generate(uint s) { var g = Genome.Generate(s, current.Family); if (lockAnatomy) g = g with { Mass = current.Mass, Length = current.Length, Legs = current.Legs, Tail = current.Tail, Head = current.Head, Horns = current.Horns }; if (lockColor) g = g with { Hue = current.Hue }; if (lockPattern) g = g with { Pattern = current.Pattern }; Apply(g); }
    void Apply(Genome genome, bool remember = true) { genome.Validate(); if (remember) history.Push(current); current = genome; syncing = true; seed.Text = current.Seed.ToString(); family.Selected = Plans.All.FindIndex(p => p.Id == current.Family); foreach (var (s, get) in sliders) s.Value = get(current); syncing = false; view.SetGenome(current); title.Text = Plans.Get(current.Family).Label + "   /   #" + current.Seed; RefreshChildren(); }
    void RefreshChildren() { if (children == null) return; foreach (var n in children.GetChildren()) { children.RemoveChild(n); n.QueueFree(); } for (int i = 0; i < 5; i++) { var g = i == 0 ? current : current.Mutate(current.Seed + (uint)i * 101, mutation); var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; children.AddChild(col); var stage = new SubViewportContainer { CustomMinimumSize = new Vector2(150, 110), Stretch = true, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill }; col.AddChild(stage); var viewport = new SubViewport { Size = new Vector2I(192, 140), TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always }; stage.AddChild(viewport); var actor = new CreatureActor { Position = new Vector2(96, 102), Scale = Vector2.One }; viewport.AddChild(actor); actor.SetGenome(g); actor.SetState(MotionState.Walk); Button(i == 0 ? "PARENT A" : "Adopt child 0" + i, () => Apply(g), col); } }
    static string UserPath(string file) => ProjectSettings.GlobalizePath("user://" + (OS.GetCmdlineUserArgs().Contains("--ui-test") ? "test-" : "") + file);
    void Save() { File.WriteAllText(UserPath("genome.json"), current.ToJson()); saved = current; Notify("Saved: " + UserPath("genome.json")); }
    void Load() { Apply(Genome.FromJson(File.ReadAllText(UserPath("genome.json")))); saved = current; Notify("Saved genome loaded."); }
    void Notify(string text) => status.Text = text;
}
