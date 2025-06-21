using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AdapterTriggerResource;
using GTA;
using IniParser;
using IniParser.Model;

internal class AdapterTrigger : Script
{
    private UdpClient _senderClient;
    private IniData _config;
    private TriggerPacket _lastTriggerPacket;
    private static Ped _playerPed;
    private static Weapon _currentWeapon;
    private static Vehicle _currentVehicle;
    private static VehicleWeaponHash _vehicleWeapon;
    private static bool _isReloading;
    private static bool _forceUpdate;

    public AdapterTrigger()
    {
        const int port = 7878;
        var parser = new FileIniDataParser();
        _config = parser.ReadFile($"{AppDomain.CurrentDomain.BaseDirectory}/trigger.ini");

        SetupConnection(port);
        Tick += OnTick;
        Interval = 50;
    }

    private void OnTick(object sender, EventArgs e)
    {
        _playerPed = Game.Player.Character;
        var packet = new TriggerPacket { instructions = new Instruction[4] };

        if (CheckReloadState(packet))
        {
            SendPacket(packet);
            return;
        }

        if (CheckVehicleWeapon(packet))
        {
            SendPacket(packet);
            return;
        }

        if (CheckVehicle(packet))
        {
            SendPacket(packet);
            return;
        }

        if (CheckWeapon(packet))
        {
            SendPacket(packet);
        }
    }

    private bool CheckReloadState(TriggerPacket packet)
    {
        if (_currentWeapon == null) return false;

        bool reloading = _currentWeapon.AmmoInClip == 0 || _playerPed.IsReloading;
        if (_isReloading == reloading && !_forceUpdate) return false;

        _isReloading = reloading;
        _forceUpdate = false;

        if (!_isReloading)
        {
            _currentWeapon = null;
            _currentVehicle = null;
            _vehicleWeapon = VehicleWeaponHash.Invalid;
            return false;
        }

        packet.instructions[0] = CreateInstruction(0, "Reloading_L");
        packet.instructions[1] = CreateInstruction(1, "Reloading_R");
        return true;
    }

    private bool CheckVehicleWeapon(TriggerPacket packet)
    {
        var currentWeapon = _playerPed.VehicleWeapon;
        if (_vehicleWeapon == currentWeapon && !_forceUpdate) return false;

        _vehicleWeapon = currentWeapon;
        _currentWeapon = null;

        if (_vehicleWeapon == VehicleWeaponHash.Invalid) return false;

        string weaponName = _vehicleWeapon switch
        {
            VehicleWeaponHash.Tank => "Tank",
            VehicleWeaponHash.PlayerLaser => "PlayerLaser",
            VehicleWeaponHash.PlayerLazer => "PlayerLazer",
            VehicleWeaponHash.PlayerBullet => "PlayerBullet",
            VehicleWeaponHash.PlayerBuzzard => "PlayerBuzzard",
            VehicleWeaponHash.PlayerHunter => "PlayerHunter",
            VehicleWeaponHash.PlaneRocket => "PlaneRocket",
            VehicleWeaponHash.SpaceRocket => "SpaceRocket",
            _ => null
        };

        if (string.IsNullOrEmpty(weaponName)) return false;

        packet.instructions[0] = CreateInstruction(0, $"{weaponName}_L");
        packet.instructions[1] = CreateInstruction(1, $"{weaponName}_R");
        return true;
    }

    private bool CheckVehicle(TriggerPacket packet)
    {
        var vehicle = _playerPed.CurrentVehicle;
        if (_currentVehicle == vehicle && !_forceUpdate) return false;

        _currentVehicle = vehicle;
        _currentWeapon = null;

        if (vehicle == null) return false;

        string vehicleType = vehicle.Type switch
        {
            VehicleType.Automobile => "Automobile",
            VehicleType.Plane => "Plane",
            VehicleType.Helicopter => "Helicopter",
            VehicleType.Boat => "Boat",
            VehicleType.Motorcycle => "Motorcycle",
            VehicleType.Bicycle => "Bicycle",
            VehicleType.QuadBike => "QuadBike",
            VehicleType.Train => "Train",
            VehicleType.Trailer => "Trailer",
            VehicleType.SubmarineCar => "SubmarineCar",
            VehicleType.AmphibiousAutomobile => "AmphibiousAutomobile",
            VehicleType.AmphibiousQuadBike => "AmphibiousQuadBike",
            VehicleType.Blimp => "Blimp",
            VehicleType.Autogyro => "Autogyro",
            VehicleType.Submarine => "Submarine",
            _ => null
        };

        if (string.IsNullOrEmpty(vehicleType)) return false;

        packet.instructions[0] = CreateInstruction(0, $"{vehicleType}_L");
        packet.instructions[1] = CreateInstruction(1, $"{vehicleType}_R");
        return true;
    }

    private bool CheckWeapon(TriggerPacket packet)
    {
        if (_currentVehicle != null) return false;

        var weapon = _playerPed.Weapons.Current;
        if (_currentWeapon == weapon && !_forceUpdate) return false;

        _currentWeapon = weapon;
        if (weapon == null) return false;

        string weaponType = weapon.Group switch
        {
            WeaponGroup.Pistol => "Pistol",
            WeaponGroup.AssaultRifle => "AssaultRifle",
            WeaponGroup.Shotgun => "Shotgun",
            WeaponGroup.SMG => "SMG",
            WeaponGroup.Sniper => "Sniper",
            WeaponGroup.MG => "MG",
            WeaponGroup.Melee => "Melee",
            WeaponGroup.Heavy => "Heavy",
            WeaponGroup.Thrown => "Thrown",
            WeaponGroup.Parachute => "Parachute",
            WeaponGroup.Stungun => "Stungun",
            WeaponGroup.PetrolCan => "PetrolCan",
            WeaponGroup.Unarmed => "Unarmed",
            WeaponGroup.NightVision => "NightVision",
            WeaponGroup.DigiScanner => "DigiScanner",
            WeaponGroup.FireExtinguisher => "FireExtinguisher",
            _ => "Unarmed"
        };

        string configKey = weaponType;
        bool hasCustomConfig = _config.Sections.ContainsSection($"{weaponType}_{weapon.DisplayName}_R") &&
                               _config.Sections.ContainsSection($"{weaponType}_{weapon.DisplayName}_L");

        if (hasCustomConfig)
        {
            configKey = $"{weaponType}_{weapon.DisplayName}";
        }

        packet.instructions[0] = CreateInstruction(0, $"{configKey}_L");
        packet.instructions[1] = CreateInstruction(1, $"{configKey}_R");
        return true;
    }

    private Instruction CreateInstruction(int triggerIndex, string configKey)
    {
        // 默认指令参数
        int[] defaultParams = { 0, triggerIndex, 19, 0, 0, 0, 0, 0 };

        // 检查配置是否存在
        if (!_config.Sections.ContainsSection(configKey))
        {
            return new Instruction
            {
                type = (InstructionType)1,
                parameters = defaultParams
            };
        }

        // 安全读取配置值
        var section = _config[configKey];
        return new Instruction
        {
            type = (InstructionType)1,
            parameters = new[]
            {
                0,
                triggerIndex,
                19,
                GetConfigValue(section, "Mode", 0),
                GetConfigValue(section, "param1", 0),
                GetConfigValue(section, "param2", 0),
                GetConfigValue(section, "param3", 0),
                GetConfigValue(section, "param4", 0)
            }
        };
    }

    private int GetConfigValue(KeyDataCollection section, string key, int defaultValue)
    {
        return section.ContainsKey(key) && int.TryParse(section[key], out int result)
            ? result : defaultValue;
    }

    private void SendPacket(TriggerPacket packet)
    {
        // 检查指令是否有效
        if (IsInstructionEmpty(packet.instructions[0]) || IsInstructionEmpty(packet.instructions[1]))
        {
            return;
        }

        // 仅在_lastTriggerPacket非空时比较
        if (_lastTriggerPacket != null && PacketEquals(packet, _lastTriggerPacket))
        {
            return;
        }

        _lastTriggerPacket = packet;
        string json = Triggers.PacketToJson(packet);

        try
        {
            byte[] data = Encoding.ASCII.GetBytes(json);
            _senderClient.Send(data, data.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send error: {ex.Message}");
        }
    }

    // 检查指令是否为空
    private bool IsInstructionEmpty(Instruction instruction)
    {
        return instruction.parameters == null || instruction.parameters.Length == 0;
    }

    private bool PacketEquals(TriggerPacket a, TriggerPacket b)
    {
        if (a == null || b == null) return false;
        if (a.instructions == null || b.instructions == null) return false;
        if (a.instructions.Length != b.instructions.Length) return false;

        for (int i = 0; i < a.instructions.Length; i++)
        {
            Instruction instA = a.instructions[i];
            Instruction instB = b.instructions[i];

            // 检查两个指令是否都为空
            if (IsInstructionEmpty(instA) && IsInstructionEmpty(instB))
                continue;

            // 如果其中一个为空而另一个不是，则不相等
            if (IsInstructionEmpty(instA) || IsInstructionEmpty(instB))
                return false;

            // 比较类型
            if (instA.type != instB.type)
                return false;

            // 检查参数数组
            if (instA.parameters == null || instB.parameters == null)
                return false;

            if (instA.parameters.Length != instB.parameters.Length)
                return false;

            // 比较参数值
            for (int j = 0; j < instA.parameters.Length; j++)
            {
                if (instA.parameters[j] != instB.parameters[j])
                    return false;
            }
        }

        return true;
    }

    private void SetupConnection(int port)
    {
        try
        {
            _senderClient = new UdpClient();
            var endpoint = new IPEndPoint(Triggers.localhost, port);
            _senderClient.Connect(endpoint);
            _senderClient.Client.SendTimeout = 10;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
        }
    }
}