using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SourcetreeForceGBK
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string targetDir = Path.Combine(baseDir, "SourceTree");
            string targetFileName = "SourceTree.Api.UI.Wpf.dll";
            string targetFilePath = null;
            if (Directory.Exists(targetDir))
            {
                string[] foundFiles = Directory.GetFiles(targetDir, targetFileName, SearchOption.AllDirectories);
                foreach (string file in foundFiles)
                {
                    targetFilePath = file;
                    break;
                }
            }
            Console.WriteLine($"请输入需要打补丁的SourceTree.Api.UI.Wpf.dll的路径(可拖入dll文件)(默认：{targetFilePath}，回车使用默认)：");
            var fileName = Console.ReadLine();
            fileName = string.IsNullOrWhiteSpace(fileName) ? targetFilePath : fileName;
            // 加载目标程序集（可以是任何 .exe 或 .dll 文件）
            var module = ModuleDefMD.Load(fileName);  // 目标程序集
            var processType = module.Types.First(t => "SourceTree.Utils.RepoProcess".Equals(t.FullName)); // 替换成目标类名
            var method = processType.Methods.First(m => "System.Void SourceTree.Utils.RepoProcess::InternalLaunch(System.Boolean,System.Diagnostics.ProcessPriorityClass)".Equals(m.FullName));
            var instructions = method.Body.Instructions;

            var isPatch = instructions.Any(i => OpCodes.Ldstr.Equals(i.OpCode) && " -- ".Equals(i.Operand));

            if (isPatch)
            {
                Console.WriteLine("已打完补丁，不用重复打补丁！");
                Console.ReadKey();
                return;
            }

            var instructionStartIndex = -1;
            Instruction instructionEnd = null;
            for (var i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];
                if (OpCodes.Callvirt.Equals(instruction.OpCode) && "System.Collections.Specialized.StringDictionary System.Diagnostics.ProcessStartInfo::get_EnvironmentVariables()".Equals(((IFullName)instruction.Operand).FullName))
                {
                    instructionStartIndex = i - 2;
                    var instructionEndIndex = instructionStartIndex;
                    instructionEnd = instructions[instructionEndIndex];
                    break;
                }
            }

            if (instructionStartIndex < 0 || instructionEnd == null)
            {
                Console.WriteLine("代码注入失败，请联系作者！");
                Console.ReadKey();
                return;
            }

            // 构造插入的 IL 指令
            var insertInstructions = new Instruction[]
            {
                // if (process.StartInfo.Arguments != null)
                Instruction.Create(OpCodes.Nop), // Nop
                Instruction.Create(OpCodes.Ldloc_1), // 加载 `process`
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.Process).GetProperty("StartInfo").GetGetMethod())),
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.ProcessStartInfo).GetProperty("Arguments").GetGetMethod())),
                Instruction.Create(OpCodes.Brfalse_S, instructionEnd), // 如果 Arguments 为 null，跳过后面的逻辑
            
                // if (Arguments.Contains(" -- "))
                Instruction.Create(OpCodes.Nop), // Nop
                Instruction.Create(OpCodes.Ldloc_1), // 加载 `process`
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.Process).GetProperty("StartInfo").GetGetMethod())),
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.ProcessStartInfo).GetProperty("Arguments").GetGetMethod())),
                Instruction.Create(OpCodes.Ldstr, " -- "),  // 加载字符串 " -- "
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(string).GetMethod("Contains", new[] { typeof(string) }))),
                Instruction.Create(OpCodes.Brfalse_S, instructionEnd), // 如果 Arguments 不包含 " -- "，跳过后面的逻辑
            
                // process.StartInfo.StandardOutputEncoding = Encoding.Default;
                Instruction.Create(OpCodes.Nop), // Nop
                Instruction.Create(OpCodes.Ldloc_1), // 加载 `process`
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.Process).GetProperty("StartInfo").GetGetMethod())),
                Instruction.Create(OpCodes.Call, module.Import(typeof(System.Text.Encoding).GetProperty("Default").GetGetMethod())), // 获取 Encoding.Default
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.ProcessStartInfo).GetProperty("StandardOutputEncoding").GetSetMethod())),
            
                // process.StartInfo.StandardErrorEncoding = Encoding.Default;
                Instruction.Create(OpCodes.Nop), // Nop
                Instruction.Create(OpCodes.Ldloc_1), // 加载 `process`
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.Process).GetProperty("StartInfo").GetGetMethod())),
                Instruction.Create(OpCodes.Call, module.Import(typeof(System.Text.Encoding).GetProperty("Default").GetGetMethod())), // 获取 Encoding.Default
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.ProcessStartInfo).GetProperty("StandardErrorEncoding").GetSetMethod()))
            };
            
            // 插入新指令
            foreach (var insertInstruction in insertInstructions.Reverse()) // 逆序插入
            {
                instructions.Insert(instructionStartIndex, insertInstruction);
            }
            
            // 自动修复跳转范围问题
            method.Body.SimplifyBranches();
            method.Body.OptimizeBranches();
            
            // 原始dll文件重命名保留
            File.Move(fileName, fileName + ".bak");
            
            // 保存修改后的程序集
            module.Write(fileName);  // 保存为新的文件
            Console.WriteLine("补丁已成功打上！");
            Console.ReadKey();
        }
    }
}