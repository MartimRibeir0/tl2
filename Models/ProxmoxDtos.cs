namespace KubeManager.API.Models;

public record VirtualMachineDto(int VmId, string Name, string Status, string Node, double Cpu, long Memory, long Uptime);
public record VmActionDto(string Action);
public record CloneVmDto(int NewId, string Name, string TargetNode, bool Full = true);
