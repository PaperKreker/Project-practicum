void BlurImitation_float(UnityTexture2D Tex, UnitySamplerState SS, float2 UV, float BlurSize, out float4 OutColor)
{
    // Те самые веса от тройного Box Blur (сумма = 27 для 1D, 729 для 2D)
    float weights[7] = { 1.0, 3.0, 6.0, 7.0, 6.0, 3.0, 1.0 };
    float4 result = float4(0, 0, 0, 0);

    // Двойной цикл для сетки 7x7
    for (int x = -3; x <= 3; x++)
    {
        for (int y = -3; y <= 3; y++)
        {
            float2 offset = float2(x, y) * BlurSize;
            float weight = weights[x + 3] * weights[y + 3];
            
            // Берем пиксель и умножаем на его вес
            result += Tex.Sample(SS, UV + offset) * weight;
        }
    }
    
    for (int x = -3; x <= 3; x++)
    {
        for (int y = -3; y <= 3; y++)
        {
            float2 offset = float2(x, y) * BlurSize;
            float weight = weights[x + 3] * weights[y + 3];
            
            // Берем пиксель и умножаем на его вес
            result += Tex.Sample(SS, UV + offset) * weight;
        }
    }
    
    
    for (int x = -3; x <= 3; x++)
    {
        for (int y = -3; y <= 3; y++)
        {
            float2 offset = float2(x, y) * BlurSize;
            float weight = weights[x + 3] * weights[y + 3];
            
            // Берем пиксель и умножаем на его вес
            result += Tex.Sample(SS, UV + offset) * weight;
        }
    }
    
    // Делим на общую сумму весов (27 * 27)
    OutColor = result / 729.0 / 3;
}
