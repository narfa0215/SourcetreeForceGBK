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
            if (string.IsNullOrWhiteSpace(fileName))
            {
                if (string.IsNullOrWhiteSpace(targetFilePath))
                {
                    Console.WriteLine("默认SourceTree.Api.UI.Wpf.dll路径不可用！");
                    Console.ReadKey();
                    return;
                }
                fileName = targetFilePath;
            }

            var loadFileName = fileName;
            if (File.Exists(fileName + ".bak"))
            {
                loadFileName += ".bak";
            }
            // 加载目标程序集（可以是任何 .exe 或 .dll 文件）
            ModuleDefMD module = null;  // 目标程序集
            try
            {
                module = ModuleDefMD.Load(loadFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("文件似乎被占用，请关闭Sourcetree再运行，错误信息如下：");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.ReadKey();
                return;
            }
            var processType = module.Types.First(t => "SourceTree.Utils.RepoProcess".Equals(t.FullName)); // 替换成目标类名
            var method = processType.Methods.First(m => "System.Void SourceTree.Utils.RepoProcess::InternalLaunch(System.Boolean,System.Diagnostics.ProcessPriorityClass)".Equals(m.FullName));
            var instructions = method.Body.Instructions;

            var isPatch1 = instructions.Any(i => OpCodes.Ldstr.Equals(i.OpCode) && " -- ".Equals(i.Operand));
            var isPatch2 = instructions.Any(i => OpCodes.Ldstr.Equals(i.OpCode) && ("APP.ini".Equals(i.Operand) || ".project".Equals(i.Operand)));

            if (isPatch1 && isPatch2)
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

            var instructionEncodingStart = Instruction.Create(OpCodes.Ldloc_1);

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
                
                // if (File.Exists(Path.Combine(process.StartInfo.WorkingDirectory, "APP.ini")))
                Instruction.Create(OpCodes.Nop), // Nop
                Instruction.Create(OpCodes.Ldloc_1), // 加载 `process`
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.Process).GetProperty("StartInfo").GetGetMethod())),
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.ProcessStartInfo).GetProperty("WorkingDirectory").GetGetMethod())),
                Instruction.Create(OpCodes.Ldstr, "APP.ini"),  // 加载字符串 "APP.ini"
                Instruction.Create(OpCodes.Call, module.Import(typeof(Path).GetMethod("Combine", new[] { typeof(string), typeof(string) }))),
                Instruction.Create(OpCodes.Call, module.Import(typeof(File).GetMethod("Exists", new[] { typeof(string) }))),
                Instruction.Create(OpCodes.Brtrue_S, instructionEncodingStart), // 如果 APP.ini 存在，继续执行
                
                // if (File.Exists(Path.Combine(process.StartInfo.WorkingDirectory, ".project")))
                Instruction.Create(OpCodes.Nop), // Nop
                Instruction.Create(OpCodes.Ldloc_1), // 加载 `process`
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.Process).GetProperty("StartInfo").GetGetMethod())),
                Instruction.Create(OpCodes.Callvirt, module.Import(typeof(System.Diagnostics.ProcessStartInfo).GetProperty("WorkingDirectory").GetGetMethod())),
                Instruction.Create(OpCodes.Ldstr, ".project"),  // 加载字符串 ".project"
                Instruction.Create(OpCodes.Call, module.Import(typeof(Path).GetMethod("Combine", new[] { typeof(string), typeof(string) }))),
                Instruction.Create(OpCodes.Call, module.Import(typeof(File).GetMethod("Exists", new[] { typeof(string) }))),
                Instruction.Create(OpCodes.Brfalse_S, instructionEnd), // 如果 .project 存在，继续执行
            
                // process.StartInfo.StandardOutputEncoding = Encoding.Default;
                Instruction.Create(OpCodes.Nop), // Nop
                instructionEncodingStart, // 加载 `process`
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
            if (!loadFileName.EndsWith(".bak") && !isPatch1)
            {
                File.Move(fileName, fileName + ".bak");
            }
            // 保存修改后的程序集
            try
            {
                module.Write(fileName);  // 保存为新的文件
            }
            catch (Exception ex)
            {
                Console.WriteLine("文件似乎被占用，请关闭Sourcetree再运行，错误信息如下：");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.ReadKey();
                return;
            }
            Console.WriteLine("补丁已成功打上！");
            Console.ReadKey();
        }
    }
}