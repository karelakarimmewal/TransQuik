using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TranQuik.Model;

public class HeldCartManager
{
    private string filePath = "held_carts.json";

    public void SaveHeldCarts(Dictionary<DateTime, HeldCart> heldCarts)
    {
        string json = JsonSerializer.Serialize(heldCarts);
        File.WriteAllText(filePath, json);
    }

    public Dictionary<DateTime, HeldCart> LoadHeldCarts()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<DateTime, HeldCart>>(json);
        }
        else
        {
            return new Dictionary<DateTime, HeldCart>();
        }
    }
}
