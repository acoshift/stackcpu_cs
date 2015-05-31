using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stack_CPU
{
    class StackCPU
    {
        private int[] Mem, ftMem;

        public StackCPU()
        {
            Lines = new List<string>();
            DataStack = new List<int>();
            ReturnStack = new List<int>();
            LastError = "";
            LastErrorAddr = 0;
            MemSize = 32;
            Mem = new int[MemSize];
            ftMem = new int[MemSize];
            PC = -1;
            Halt = true;
        }

        public bool Compile()
        {
            LineReconstruct();
            if (!Preprocessing()) return false;
            if (!Processing()) return false;
            return true;
        }

        public void ClearStack()
        {
            DataStack.Clear();
            ReturnStack.Clear();
            PC = 0;
            Halt = false;
            Mem = new int[MemSize];
            Array.Copy(ftMem, Mem, MemSize);
        }

        public bool Run()
        {
            while (!Halt)
                if (!Step()) return false;
            return true;
        }

        public bool StepInto()
        {
            return Step();
        }

        public bool StepOver()
        {
            int t = Mem[PC] == 0xff0d ? 1 : 0;
            if (!Step()) return false;
            while (!Halt && t > 0)
            {
                if (Mem[PC] == 0xff0d) ++t;
                else if (Mem[PC] == 0xff0e) --t;
                if (!Step()) return false;
            }
            return true;
        }

        public int Memory(int i)
        {
            return Mem[i];
        }

        public List<string> Lines { get; set; }
        public string LastError { get; private set; }
        public int LastErrorAddr { get; private set; }
        public int PC { get; private set; }
        public List<int> DataStack { get; private set; }
        public List<int> ReturnStack { get; private set; }
        public int MemSize { get; set; }
        public bool Halt { get; private set; }

        private int GetMem(int addr)
        {
            if (addr < 0 || addr >= MemSize) return 0;
            return Mem[addr];
        }

        private void SetMem(int addr, int val)
        {
            if (addr >= 0 && addr < MemSize) Mem[addr] = val;
        }

        private bool Push(List<int> stack, int val)
        {
            if (stack.Count < MAXSTACK)
            {
                stack.Add(val);
                return true;
            }
            return false;
        }

        private int Pop(List<int> stack)
        {
            if (stack.Count > 0)
            {
                int top = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                return top;
            }
            return 0xffff;
        }

        private int Peek(List<int> stack)
        {
            if (stack.Count > 0)
            {
                int top = stack[stack.Count - 1];
                return top;
            }
            return 0xffff;
        }

        private void LineReconstruct()
        {
            List<string> l = new List<string>();;
            string s, t;
            for (int i = 0; i < Lines.Count; ++i)
            {
                s = Lines[i];
                t = "";
		        for (int j = 0; j < s.Length; ++j)
                {
                    if (s[j] == ' ' || s[j] == '\t')
                    {
                        if (t != "") l.Add(t.ToUpper());
                        t = "";
                        continue;
                    }
                    if (s[j] == ';') break;
                    t += s[j];
                }
                if (t != "") l.Add(t.ToUpper());
            }
            Lines.Clear();
            Lines.AddRange(l);
        }

        private bool Preprocessing()
        {
            List<Opcode> m = new List<Opcode>();
            string s;
            int l;

            // Find Label and convert number to decimal
	        for (int i = 0; i < Lines.Count;)
            {
                s = Lines[i];
                if (!s.StartsWith(":"))
                {
                    int j = opGetPci(s);
                    for (int k = 1; k < j; ++k)
                    {
				        if (i + k >= Lines.Count) break;
                        s = Lines[i + k];
                        if (!s.StartsWith(":"))
                        {
                            if (TryNumToInt(s, out l))
                            {
                                if ((Math.Abs(l) & 0xff00) != 0)
                                {
                                    LastError = string.Format(E004, s);
                                    LastErrorAddr = i + k;
                                    return false;
                                }
                                Lines[i + k] = l.ToString();
                            }
                            else
                            {
						        LastError = string.Format(E002, s);
                                LastErrorAddr = i + k;
                                return false;
                            }
                        }
                    }
                    if (j != 0) i += j; else ++i;
                    continue;
                }
                else
                {
                    // check dup
			        for (int j = 0; j < m.Count; ++j)
                    {
                        if (m[j].ops == s)
                        {
					        LastError = string.Format(E001, s);
                            LastErrorAddr = i;
                            return false;
                        }
                    }
                    Opcode t = new Opcode();
                    t.ops = s;
                    t.opc = i;
                    m.Add(t);
                    Lines.RemoveAt(i);
                }
            }

            // replace label
	        for (int i = 0; i < Lines.Count; ++i)
            {
                s = Lines[i];
                if (s.StartsWith(":"))
                {
			        for (int j = 0; j < m.Count; ++j)
                    {
                        if (m[j].ops == s)
                        {
                            Lines[i] = m[j].opc.ToString();
                            break;
                        }
                    }
                }
            }
            return true;
        }

        private bool Processing()
        {
            List<int> d = new List<int>();
            string s;
            int op, l;

	        for (int i = 0; i < Lines.Count; ++i)
            {
                s = Lines[i];
                op = opGetOpc(s);
                if (op != 0xffff) d.Add(op);
                else
                {
                    if (TryNumToInt(s, out l)) d.Add(l);
                    else
                    {
                        if (s.StartsWith(":"))
                        {
					        LastError = string.Format(E003, s);
                        }
                        else
                        {
					        LastError = string.Format(E002, s);
                        }
                        LastErrorAddr = i;
                        return false;
                    }
                }
            }

            l = MemSize;
            ftMem = new int[l];
	        for (int i = 0; i < d.Count; ++i)
            {
		        if (i < l) ftMem[i] = d[i];
                else
                {
                    LastError = E008;
                    LastErrorAddr = i;
                    return false;
                }
            }
            return true;
        }

        private bool Step()
        {
            bool ret = false;
            bool nop = false;
            int addr, tmp1, tmp2;
            int s = Mem[PC];
            switch (s)
            {
            case 0xff00:
                ret = Push(DataStack, GetMem(PC + 1));
                break;
            case 0xff01:
                addr = Pop(DataStack);
                ret = (addr != 0xffff) && Push(DataStack, GetMem(addr));
                break;
            case 0xff02:
                addr = Pop(DataStack);
                ret = addr != 0xffff;
                SetMem(addr, Pop(DataStack));
                break;
            case 0xff03:
                ret = Pop(DataStack) != 0xffff;
                break;
            case 0xff04:
                tmp1 = Peek(DataStack);
                ret = (tmp1 != 0xffff) && Push(DataStack, tmp1);
                break;
            case 0xff05:
                tmp1 = Pop(DataStack);
                tmp2 = Peek(DataStack);
                Push(DataStack, tmp1);
                ret = (tmp2 != 0xffff) && Push(DataStack, tmp2);
                break;
            case 0xff06:
                tmp1 = Pop(DataStack);
                tmp2 = Pop(DataStack);
                Push(DataStack, tmp1);
                ret = (tmp2 != 0xffff) && Push(DataStack, tmp2);
                break;
            case 0xff07:
                tmp2 = Pop(DataStack);
                tmp1 = Pop(DataStack);
                ret = (tmp1 != 0xffff) && Push(DataStack, tmp1 + tmp2);
                break;
            case 0xff08:
                tmp2 = Pop(DataStack);
                tmp1 = Pop(DataStack);
                ret = (tmp1 != 0xffff) && Push(DataStack, tmp1 - tmp2);
                break;
            case 0xff09:
                tmp2 = Pop(DataStack);
                tmp1 = Pop(DataStack);
                ret = (tmp1 != 0xffff) && Push(DataStack, tmp1 & tmp2);
                break;
            case 0xff0a:
                tmp2 = Pop(DataStack);
                tmp1 = Pop(DataStack);
                ret = (tmp1 != 0xffff) && Push(DataStack, tmp1 | tmp2);
                break;
            case 0xff0b:
                tmp2 = Pop(DataStack);
                tmp1 = Pop(DataStack);
                ret = (tmp1 != 0xffff) && Push(DataStack, tmp1 ^ tmp2);
                break;
            case 0xff0c:
                tmp1 = Pop(DataStack);
                ret = tmp1 != 0xffff;
                if (tmp1 == 0)
                {
                    PC = GetMem(PC + 1);
                    s = 0xf;
                }
                break;
            case 0xff0d:
                ret = Push(ReturnStack, PC + 2);
                PC = GetMem(PC + 1);
                s = 0xf;
                break;
            case 0xff0e:
                PC = Pop(ReturnStack);
                ret = PC != 0xffff;
                break;
            case 0xff0f:
                Halt = true;
                ret = true;
                break;
            case 0xff10:
                tmp1 = Pop(DataStack);
                ret = (tmp1 != 0xffff) && Push(ReturnStack, tmp1);
                break;
            case 0xff11:
                tmp1 = Pop(ReturnStack);
                ret = (tmp1 != 0xffff) && Push(DataStack, tmp1);
                break;
            default:
               nop = true;
               break;
            }
            if (ret) PC += opGetPci(s);
            else
            {
                if (!nop) LastError = E005;
                else
                {
                    LastError = string.Format(E007, (byte)s);
                }
                LastErrorAddr = PC;
            }
            if (PC >= MemSize)
            {
                ret = false;
                LastError = E006;
                LastErrorAddr = PC;
            }
            return ret;
        }

        private const int MAXSTACK = 0xff;
        private const int OPL = 0x12;

        private const string E001 = "Label redecleared: {0}";
        private const string E002 = "\"{0}\" is not a valid integer value";
        private const string E003 = "Undeclared label: {0}";
        private const string E004 = "Constant value violates subrange bounDataStack: {0}";
        private const string E005 = "Stack overflow/underflow";
        private const string E006 = "PC out of bounDataStack";
        private const string E007 = "\"x{0,2}\" is not an opcode";
        private const string E008 = "Out of memory";

        private static readonly Opcode[] opcodes = new Opcode[OPL]
        {
            new Opcode("LIT",     0xff00, 2),
            new Opcode("@",       0xff01, 1),
            new Opcode("!",       0xff02, 1),
            new Opcode("DROP",    0xff03, 1),
            new Opcode("DUP",     0xff04, 1),
            new Opcode("OVER",    0xff05, 1),
            new Opcode("SWAP",    0xff06, 1),
            new Opcode("+",       0xff07, 1),
            new Opcode("-",       0xff08, 1),
            new Opcode("AND",     0xff09, 1),
            new Opcode("OR",      0xff0a, 1),
            new Opcode("XOR",     0xff0b, 1),
            new Opcode("IF",      0xff0c, 2),
            new Opcode("CALL",    0xff0d, 2),
            new Opcode("EXIT",    0xff0e, 0),
            new Opcode("HALT",    0xff0f, 0),
            new Opcode(">R",      0xff10, 1),
            new Opcode("R>",      0xff11, 1),
        };

        private struct Opcode
        {
            public string ops;
            public int opc;
            public int pci;

            public Opcode(string _ops, int _opc, int _pci)
            {
                ops = _ops;
                opc = _opc;
                pci = _pci;
            }
        }

        public static int opGetPci(string ops)
        {
            foreach (Opcode op in opcodes)
                if (op.ops == ops)
                    return op.pci;
            return 0;
        }

        public static int opGetPci(int opc)
        {
            foreach (Opcode op in opcodes)
                if (op.opc == opc)
                    return op.pci;
            return 0;
        }

        public static int opGetOpc(string ops)
        {
            foreach (Opcode op in opcodes)
                if (op.ops == ops)
                    return op.opc;
            return 0xffff;
        }

        public static string opGetOps(int opc)
        {
            foreach (Opcode op in opcodes)
                if (op.opc == opc)
                    return op.ops;
            return "";
        }

        public static int opGetCode(int opc)
        {
            if ((opc & 0xff00) == 0xff00) return opc & 0x00ff;
            return 0xffff;
        }

        private bool TryNumToInt(string num, out int val)
        {
            bool ok = true;
            val = 0;
            try
            {
                if (num.StartsWith("B"))
                    val = Convert.ToInt32(num.Substring(1, num.Length - 1), 2);
                else if (num.StartsWith("0X"))
                    val = Convert.ToInt32(num.Substring(2, num.Length - 2), 16);
                else if (num.StartsWith("X"))
                    val = Convert.ToInt32(num.Substring(1, num.Length - 1), 16);
                else
                    val = Convert.ToInt32(num, 10);
            }
            catch (Exception)
            {
                ok = false;
            }
            return ok;
        }
    }
}
