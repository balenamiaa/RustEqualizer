/*

message_handler!(context::Context, message::ChangeWeapon) = context.current_weapon = message.new_weapon
message_handler!(context::Context, message::ChangeAttachment) = context.current_attachment = message.new_attachment
message_handler!(context::Context, _::ToggleEnabled) = context.enabled = !context.enabled

function reset!(info::TransientRecoilInformation)
    info.num_shots_fired = 0
    info.current_viewangles = zero(Vec2d)
end

function _calculate_punch_curve(player_status::PlayerStatus, num_shots_fired, nbullets)
    curve = Weapon.WEAPON_ANIMATIONCURVE[][player_status.weapon_kind]
    scalar = Weapon.WEAPON_BASELINE_SCALAR[][player_status.weapon_kind] * Weapon.ATTACHMENT_RCS_MULTIPLIER[][player_status.adsattachment_kind]

    t = num_shots_fired / nbullets
    vec2(curve.yaw(t), curve.pitch(t)) .* scalar
end

function _calculate_punch_nocurve(player_status::PlayerStatus)
    scalar =
        (player_status.is_ducked ? 0.5 : 1.0) *
        (player_status.is_ads ? Weapon.WEAPON_ADSSCALE[][player_status.weapon_kind] : 1.0) *
        Weapon.ATTACHMENT_RCS_MULTIPLIER[][player_status.adsattachment_kind] *
        (player_status.is_moving ? (1.0 + Weapon.WEAPON_MOVEMENT_PENALTY[][player_status.weapon_kind]) : 1.0)

    Weapon.WEAPON_BASELINE_SCALAR[][player_status.weapon_kind] .* scalar
end

function control_recoil(context::Context, info::TransientRecoilInformation)
    is_curvebased = Weapon.WEAPON_ISCURVEBASED[][context.current_weapon]
    _control_recoil(context, info, Val(is_curvebased))
end

function _control_recoil(context::Context, info::TransientRecoilInformation, is_curvebased::Val{true})
    time_per_shot_ms = Weapon.WEAPON_TIMEPERSHOT_MS[][context.current_weapon]
    nbullets = Weapon.WEAPON_NUM_BULLETS[][context.current_weapon]

    delta_last_shot_time_ms = WinApi.time_ms() - info.last_shot_time_ms
    (delta_last_shot_time_ms > 2 * time_per_shot_ms) && reset!(info)

    while WinApi.check_key_down(WinApi.VK_LBUTTON)
        shoot!(info, nbullets) #first shot already fired

        player_status = playerstatus(context)
        game_status = gamestatus(context)

        punch = _calculate_punch_curve(player_status, info.num_shots_fired, nbullets)
        delta_angle = map(-, punch) - info.current_viewangles
        control_time = round(Int64, LinearAlgebra.norm(delta_angle) / 0.02)

        overflow_counter = zero(Vec2d)

        current_time = Int64(0)
        prev_time = Int64(0)
        timer = WinApi.WinTimer()

        while current_time < control_time
            current_time = WinApi.elapsed_ms(timer)
            delta_time_ms = current_time - prev_time

            delta_angle_chunk = delta_angle * (delta_time_ms / control_time)
            delta_pixel_chunk = δangle_to_δpixel(delta_angle_chunk, game_status, player_status)
            overflow_counter = move_mouse_overflow(delta_pixel_chunk, overflow_counter)

            prev_time = current_time
        end

        info.current_viewangles = info.current_viewangles + delta_angle

        excess = time_per_shot_ms - WinApi.elapsed_ms(timer)
        excess > 0 && WinApi.sleep_ms(excess)
    end
end

function _control_recoil(context::Context, info::TransientRecoilInformation, is_curvebased::Val{false})
    is_auto = Weapon.WEAPON_ISAUTOMATIC[][context.current_weapon]
    __control_recoil_nocurve(context, info, Val(is_auto))
end

function __control_recoil_nocurve(context::Context, info::TransientRecoilInformation, is_auto::Val{true})
    time_per_shot_ms = Weapon.WEAPON_TIMEPERSHOT_MS[][context.current_weapon]
    control_time = time_per_shot_ms

    while WinApi.check_key_down(WinApi.VK_LBUTTON)

        info.current_time_ms = WinApi.time_ms()
        if info.current_time_ms - info.last_time_ms < time_per_shot_ms
            return
        end

        player_status = playerstatus(context)
        game_status = gamestatus(context)

        punch = _calculate_punch_nocurve(player_status)
        delta_angle = map(-, punch)

        overflow_counter = zero(Vec2d)

        current_time = Int64(0)
        prev_time = Int64(0)
        timer = WinApi.WinTimer()

        while current_time < control_time
            current_time = WinApi.elapsed_ms(timer)
            delta_time_ms = current_time - prev_time

            delta_angle_chunk = delta_angle * (delta_time_ms / control_time)
            delta_pixel_chunk = δangle_to_δpixel(delta_angle_chunk, game_status, player_status)
            overflow_counter = move_mouse_overflow(delta_pixel_chunk, overflow_counter)

            prev_time = current_time
        end
        info.last_time_ms = info.current_time_ms
        info.current_viewangles = info.current_viewangles + delta_angle
    end
end

function __control_recoil_nocurve(context::Context, info::TransientRecoilInformation, is_auto::Val{false})
    time_per_shot_ms = Weapon.WEAPON_TIMEPERSHOT_MS[][context.current_weapon]
    control_time = time_per_shot_ms
    
    while WinApi.check_key_down(WinApi.VK_LBUTTON)
        WinApi.sleep_ms(5)
        send_repeat_button(context)

        info.current_time_ms = WinApi.time_ms()
        if info.current_time_ms - info.last_time_ms < time_per_shot_ms
            return
        end

        player_status = playerstatus(context)
        game_status = gamestatus(context)

        punch = _calculate_punch_nocurve(player_status)
        delta_angle = map(-, punch)

        overflow_counter = zero(Vec2d)

        current_time = Int64(0)
        prev_time = Int64(0)
        timer = WinApi.WinTimer()

        while current_time < control_time
            current_time = WinApi.elapsed_ms(timer)
            delta_time_ms = current_time - prev_time

            delta_angle_chunk = delta_angle * (delta_time_ms / control_time)
            delta_pixel_chunk = δangle_to_δpixel(delta_angle_chunk, game_status, player_status)
            overflow_counter = move_mouse_overflow(delta_pixel_chunk, overflow_counter)

            prev_time = current_time
        end
        info.last_time_ms = info.current_time_ms
        info.current_viewangles = info.current_viewangles + delta_angle
    end
end

function main()
    config_fileio = open("config.json", "r")
    userdata::UserData = JSON3.read(config_fileio, UserData)
    close(config_fileio)

    context = Context(;
        current_attachment = Weapon.ATTACHMENT_NONE,
        current_weapon = Weapon.WEAPON_AK47, sensitivity = userdata.game.sensitivity,
        fov = userdata.game.fov, keys_down = zero(MVector{NUM_KEYBINDS,Bool}),
        prev_keys_down = zero(MVector{NUM_KEYBINDS,Bool}), keys_idx_map = SA[
            userdata.keybinds.ak47, userdata.keybinds.lightrifle, userdata.keybinds.m249,
            userdata.keybinds.mp5, userdata.keybinds.thompson, userdata.keybinds.customsmg,
            userdata.keybinds.semirifle, userdata.keybinds.revolver, userdata.keybinds.nailgun,
            userdata.keybinds.pythonrevolver, userdata.keybinds.semipistol, userdata.keybinds.m39, userdata.keybinds.m92,
            userdata.keybinds.noscope, userdata.keybinds.holo, userdata.keybinds.scope8, userdata.keybinds.scope16,
            WinApi.VK_LBUTTON, WinApi.VK_HOME
        ],
        messages_map = SA[
            ChangeWeapon(Weapon.WEAPON_AK47),
            ChangeWeapon(Weapon.WEAPON_LIGHTRIFLE),
            ChangeWeapon(Weapon.WEAPON_M249),
            ChangeWeapon(Weapon.WEAPON_MP5),
            ChangeWeapon(Weapon.WEAPON_THOMPSON),
            ChangeWeapon(Weapon.WEAPON_CUSTOMSMG),
            ChangeWeapon(Weapon.WEAPON_SEMIRIFLE),
            ChangeWeapon(Weapon.WEAPON_REVOLVER),
            ChangeWeapon(Weapon.WEAPON_NAILGUN),
            ChangeWeapon(Weapon.WEAPON_PYTHONREVOLVER),
            ChangeWeapon(Weapon.WEAPON_SEMIPISTOL),
            ChangeWeapon(Weapon.WEAPON_M39),
            ChangeWeapon(Weapon.WEAPON_M92),
            ChangeAttachment(Weapon.ATTACHMENT_NONE),
            ChangeAttachment(Weapon.ATTACHMENT_HOLO),
            ChangeAttachment(Weapon.ATTACHMENT_SCOPE8),
            ChangeAttachment(Weapon.ATTACHMENT_SCOPE16),
            LeftMBClicked(),
            ToggleEnabled()
        ], activation_key = userdata.keybinds.activation,
        crouch_key = userdata.keybinds.crouch,
        repeat_key = userdata.keybinds.attack,
        movement_keys = (
            userdata.keybinds.forward, userdata.keybinds.backward,
            userdata.keybinds.left, userdata.keybinds.right
        ), enabled = true
    )

    while i < 10000000
        context_begin!(context)
        context_end!(context)

        yield()
    end
end

end
```
convert the above Julia code to C#

*/


namespace RustEQ;

using System.Numerics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Win32.UI.Input.KeyboardAndMouse;

struct PlayerStatus
{
    public bool IsDucked { get; set; }
    public bool IsMoving { get; set; }
    public bool IsADS { get; set; }
    public byte WeaponKind { get; set; }
    public byte ADSAttachmentKind { get; set; }
}

struct GameStatus
{
    public float FOV { get; set; }
    public float Sensitivity { get; set; }
    public bool RCSEnabled { get; set; }
}

/*

abstract type AbstractMessage end

struct ChangeAttachment <: AbstractMessage
    new_attachment::UInt8
end

struct ChangeWeapon <: AbstractMessage
    new_weapon::UInt8
end

struct ToggleEnabled <: AbstractMessage end
struct LeftMBClicked <: AbstractMessage end

*/

public abstract record AbstractMessage { }

public record ChangeAttachment(byte NewAttachment) : AbstractMessage { }
public record ChangeWeapon(byte NewWeapon) : AbstractMessage { }
public record ToggleEnabled : AbstractMessage { }
public record LeftMBClicked : AbstractMessage { }




[JsonSerializable(typeof(KeybindsConfig))]
public class KeybindsConfig
{
    public ushort AK47 { get; } = 97;
    public ushort LightRifle { get; } = 98;
    public ushort M249 { get; } = 99;
    public ushort MP5 { get; } = 100;
    public ushort Thompson { get; } = 101;
    public ushort CustomSMG { get; } = 102;
    public ushort SemiRifle { get; } = 103;
    public ushort Revolver { get; } = 104;
    public ushort Nailgun { get; } = 105;
    public ushort PythonRevolver { get; } = 111;
    public ushort SemiPistol { get; } = 106;
    public ushort NoScope { get; } = 109;
    public ushort M39 { get; } = 123;
    public ushort M92 { get; } = 124;
    public ushort Holo { get; } = 107;
    public ushort Scope8 { get; } = 110;
    public ushort Scope16 { get; } = 96;
    public ushort Activation { get; } = 34;
    public ushort Crouch { get; } = 18;
    public ushort Attack { get; } = 187;
    public ushort Forward { get; } = 69;
    public ushort Backward { get; } = 83;
    public ushort Left { get; } = 65;
    public ushort Right { get; } = 68;
}

[JsonSerializable(typeof(GameConfig))]
public class GameConfig
{
    public float FieldOfView { get; } = 85f;
    public float Sensitivity { get; } = 0.1f;
}


[JsonSerializable(typeof(UserData))]
public class UserData
{


    public KeybindsConfig KeybindsConfig { get; }
    public GameConfig GameConfig { get; }

    public UserData()
    {
        KeybindsConfig = new();
        GameConfig = new();
    }
}


public struct TransientRecoilInformation
{
    public long NumShortsFired { get; set; }
    public long LastShotTimeMs { get; set; }
    public Vector2 CurrentViewangles { get; set; }
    public long CurrentTimeMs { get; set; }
    public long LastTimeMs { get; set; }

    public void Reset()
    {
        NumShortsFired = 0;
        CurrentViewangles = Vector2.Zero;
    }
}


[SupportedOSPlatform("windows7.0")]
public class RustEqualizer
{
    public RustEqualizer()
    {
        string configFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "config.json");
        string configJson = File.ReadAllText(configFilePath);

        UserData = JsonSerializer.Deserialize<UserData>(configJson)!;

        KeysIdxMap = new ushort[NumTransientKeybinds]
        {
            UserData.KeybindsConfig.AK47,
            UserData.KeybindsConfig.LightRifle,
            UserData.KeybindsConfig.M249,
            UserData.KeybindsConfig.MP5,
            UserData.KeybindsConfig.Thompson,
            UserData.KeybindsConfig.CustomSMG,
            UserData.KeybindsConfig.SemiRifle,
            UserData.KeybindsConfig.Revolver,
            UserData.KeybindsConfig.Nailgun,
            UserData.KeybindsConfig.PythonRevolver,
            UserData.KeybindsConfig.SemiPistol,
            UserData.KeybindsConfig.M39,
            UserData.KeybindsConfig.M92,
            UserData.KeybindsConfig.NoScope,
            UserData.KeybindsConfig.Holo,
            UserData.KeybindsConfig.Scope8,
            UserData.KeybindsConfig.Scope16,
            (ushort)VIRTUAL_KEY.VK_LBUTTON,
            (ushort)VIRTUAL_KEY.VK_HOME
        };

        CurrentKeysDown = new bool[NumTransientKeybinds];
        PreviousKeysDown = new bool[NumTransientKeybinds];
        MovementKeys = new ushort[4] { UserData.KeybindsConfig.Forward, UserData.KeybindsConfig.Backward, UserData.KeybindsConfig.Left, UserData.KeybindsConfig.Right };
        RepeatKey = UserData.KeybindsConfig.Attack;

        MessagesMap = new AbstractMessage[] {
            new ChangeWeapon(Weapons.WEAPON_AK47),
            new ChangeWeapon(Weapons.WEAPON_LIGHTRIFLE),
            new ChangeWeapon(Weapons.WEAPON_M249),
            new ChangeWeapon(Weapons.WEAPON_MP5),
            new ChangeWeapon(Weapons.WEAPON_THOMPSON),
            new ChangeWeapon(Weapons.WEAPON_CUSTOMSMG),
            new ChangeWeapon(Weapons.WEAPON_SEMIRIFLE),
            new ChangeWeapon(Weapons.WEAPON_REVOLVER),
            new ChangeWeapon(Weapons.WEAPON_NAILGUN),
            new ChangeWeapon(Weapons.WEAPON_PYTHONREVOLVER),
            new ChangeWeapon(Weapons.WEAPON_SEMIPISTOL),
            new ChangeWeapon(Weapons.WEAPON_M39),
            new ChangeWeapon(Weapons.WEAPON_M92),
            new ChangeAttachment(Weapons.ATTACHMENT_NONE),
            new ChangeAttachment(Weapons.ATTACHMENT_HOLO),
            new ChangeAttachment(Weapons.ATTACHMENT_SCOPE8),
            new ChangeAttachment(Weapons.ATTACHMENT_SCOPE16),
            new LeftMBClicked(),
            new ToggleEnabled()
        };

        GameStatus = new GameStatus();

        TransientRecoilInformation = new TransientRecoilInformation();
    }


    private static bool IsKeyDown(ushort key)
    {
        return RqWin.GetAsyncKeyState(key) != 0;
    }

    private static void SendMouseInput(int dx, int dy)
    {
        INPUT[] inputs = new INPUT[1] {
            new INPUT
            {
                type = INPUT_TYPE.INPUT_MOUSE,
                Anonymous = new INPUT._Anonymous_e__Union
                {
                    mi = new MOUSEINPUT { dx = dx, dy = dy, dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE }
                }
            }
        };

        RqWin.SendInput(inputs, 1);
    }

    private static void SendKeyboardInput(ushort key, bool keyUp)
    {
        INPUT[] inputs = new INPUT[1] {
            new INPUT
            {
                type = INPUT_TYPE.INPUT_KEYBOARD,
                Anonymous = new INPUT._Anonymous_e__Union
                {
                    ki = new KEYBDINPUT { wVk = (VIRTUAL_KEY)key, dwFlags = keyUp ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0 }
                }
            }
        };

        RqWin.SendInput(inputs, 1);
    }


    private Vector2 MoveMouseWithOverflow(Vector2 deltaChange, Vector2 overflowCounter)
    {
        Vector2 deltaChangeWithOverflow = deltaChange + overflowCounter;
        Vector2 deltaChangeWithOverflowRounded = new((float)Math.Round(deltaChangeWithOverflow.X), (float)Math.Round(deltaChangeWithOverflow.Y));

        if (deltaChangeWithOverflowRounded.LengthSquared() == 0)
        {
            return overflowCounter;
        }

        float dx = deltaChangeWithOverflowRounded.X;
        float dy = deltaChangeWithOverflowRounded.Y;

        SendMouseInput((int)dx, (int)dy);

        Vector2 newOverflowCounter = deltaChangeWithOverflow - deltaChangeWithOverflowRounded;

        return newOverflowCounter;
    }

    private Vector2 DeltaAngleToDeltaPixel(Vector2 deltaAngle)
    {
        float fov = GameStatus.FOV;
        float zoom = PlayerStatus.ADSAttachmentKind == Weapons.ATTACHMENT_NONE ? Weapons.WEAPON_DEFAULT_ADSZOOM[PlayerStatus.WeaponKind] : Weapons.ATTACHMENT_ZOOM[PlayerStatus.ADSAttachmentKind];

        fov /= PlayerStatus.IsADS ? zoom : 1;

        return deltaAngle.DivideByScalar(0.03f * (GameStatus.Sensitivity * 3) * (fov / 100));
    }


    private void SendRepeatButton()
    {
        SendKeyboardInput(RepeatKey, false);
        SendKeyboardInput(RepeatKey, true);
    }

    private void Begin()
    {
        for (int i = 0; i < NumTransientKeybinds; i++)
        {
            if (IsKeyDown(KeysIdxMap[i]))
            {
                CurrentKeysDown[i] = true;
            }
            else
            {
                CurrentKeysDown[i] = false;
            }
        }

        for (int i = 0; i < NumTransientKeybinds; i++)
        {
            if (CurrentKeysDown[i] && !PreviousKeysDown[i])
            {
                MessageHandler(MessagesMap[i]);
            }
        }
    }

    private void End()
    {
        for (int i = 0; i < NumTransientKeybinds; i++)
        {
            PreviousKeysDown[i] = CurrentKeysDown[i];
        }
    }

    private void MessageHandler(AbstractMessage abstractMessage)
    {

        if (abstractMessage is ChangeWeapon changeWeapon)
        {
            CurrentWeapon = changeWeapon.NewWeapon;
        }
        else if (abstractMessage is ChangeAttachment changeAttachment)
        {
            CurrentAttachment = changeAttachment.NewAttachment;
        }
        else if (abstractMessage is ToggleEnabled)
        {
            GameStatus = GameStatus with { RCSEnabled = !GameStatus.RCSEnabled };
        }
        else if (abstractMessage is LeftMBClicked)
        {
            if (IsActivated && GameStatus.RCSEnabled)
            {
                ControlRecoil();
            }
        }
    }

    private void ControlRecoil()
    {
        bool isCurveBased = Weapons.WEAPON_ISCURVEBASED[CurrentWeapon];

        if (isCurveBased)
        {
            ControlRecoilCurveBased();
        }
        else
        {
            ControlRecoilNotCurveBased();
        }
    }

    private void ControlRecoilCurveBased() { }
    private void ControlRecoilNotCurveBased() { }

    public void Start()
    {

    }



    private bool IsActivated => IsKeyDown(ActivationKey);
    private ushort ActivationKey { get; set; }
    private ushort RepeatKey { get; set; }

    private TransientRecoilInformation TransientRecoilInformation { get; set; }
    private bool[] CurrentKeysDown { get; set; }
    private bool[] PreviousKeysDown { get; set; }
    private ushort[] KeysIdxMap { get; set; }
    private AbstractMessage[] MessagesMap { get; set; }
    private ushort CrouchKey { get; set; }
    private ushort[] MovementKeys { get; set; }

    private byte CurrentWeapon { get; set; }
    private byte CurrentAttachment { get; set; }

    private GameStatus GameStatus { get; set; }
    private PlayerStatus PlayerStatus
    {
        get
        {
            bool is_forward_down = IsKeyDown(MovementKeys[0]);
            bool is_backward_down = IsKeyDown(MovementKeys[1]);
            bool is_left_down = IsKeyDown(MovementKeys[2]);
            bool is_right_down = IsKeyDown(MovementKeys[3]);

            return new PlayerStatus
            {
                IsDucked = IsKeyDown(CrouchKey),
                IsMoving = ((is_forward_down && !is_backward_down) || (is_backward_down && !is_forward_down)) | ((is_left_down && !is_right_down) || (is_right_down && !is_left_down)),
                IsADS = IsKeyDown((ushort)VIRTUAL_KEY.VK_RBUTTON),
                WeaponKind = CurrentWeapon,
                ADSAttachmentKind = CurrentAttachment
            };
        }
    }

    private UserData UserData { get; }

    public const int NumTransientKeybinds = 19;

}