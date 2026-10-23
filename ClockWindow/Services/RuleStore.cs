using System;
using System.IO;
using System.Text.Json;
using ClockWindow.Models;

namespace ClockWindow.Services;

public class RuleStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClockWindow");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "rules.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public List<ReminderRule> Load()
    {
        Directory.CreateDirectory(ConfigDir);

        if (!File.Exists(ConfigPath))
        {
            var defaults = CreateDefaultRules();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var rules = JsonSerializer.Deserialize<List<ReminderRule>>(json, JsonOptions);
            return rules ?? CreateDefaultRules();
        }
        catch (JsonException)
        {
            var backupPath = ConfigPath + ".bak";
            File.Copy(ConfigPath, backupPath, overwrite: true);
            var defaults = CreateDefaultRules();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(List<ReminderRule> rules)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(rules, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    private static List<ReminderRule> CreateDefaultRules()
    {
        return new List<ReminderRule>
        {
            new ReminderRule
            {
                Title = "周报时间到了！",
                DaysOfWeek = Days.Friday,
                Time = new TimeSpan(17, 0, 0),
                Enabled = true,
                Messages = new List<string>
                {
                    "又是星期五，笑容掩不住。",
                    "周五到，欢乐在招手，心花怒放。",
                    "快乐牵住你的手，轻松上心头。",
                    "心平浪静，周五的阳光驱散心头烦闷。",
                    "周五到来乐无边，转眼周末到眼前。",
                    "一周忙碌落下帷幕，把脚步放慢一点。",
                    "两袖清风心神怡，忘却劳累舒身体。",
                    "伸伸腿脚心情愉快，扭扭腰背轻松无限。",
                    "把每个睡醒后的早晨当成一件礼物。",
                    "周五要悠闲，烦恼全靠边。",
                    "周五来到，阳光也在微笑，烦恼全都跑掉。",
                    "周五已来到，记得多笑笑，烦恼全消掉。",
                    "紧绷的心松一松，让快乐如风。",
                    "匆匆的脚缓一缓，让悠闲相伴。",
                    "把心情放松一些，让快乐多一点。",
                    "愿你笑开怀，周末就在前方。",
                    "每一天都是生命中最年轻的一天。",
                    "云淡风轻，随意放飞美丽心情。",
                    "忙碌不再骚扰，疲倦等着睡觉。",
                    "愿你心情灿烂美妙，跟着轻风奔跑。",
                    "人依旧，情依旧，又到周末。",
                    "闲也行，忙也行，开心随行。",
                    "卸下一周的疲惫，周末正在等你。",
                    "收拾好心情，周末如约而至。",
                    "愿你过得逍遥，幸福花开片片。",
                    "又到周五，把快乐紧紧拥抱。",
                },
            },
        };
    }
}
