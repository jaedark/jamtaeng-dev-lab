public struct PointValue { public int X; public int Y; }
public class PointReference { public int X; public int Y; }

var value1 = new PointValue { X = 10, Y = 20 };
var value2 = value1;
value2.X = 100;
Console.WriteLine($"value: {value1.X}, {value2.X}");

var reference1 = new PointReference { X = 10, Y = 20 };
var reference2 = reference1;
reference2.X = 100;
Console.WriteLine($"reference: {reference1.X}, {reference2.X}");
