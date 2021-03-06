﻿using System.Collections.Generic;

namespace Nodify.Calculator
{
    public enum OperationType
    {
        Normal,
        Expando,
        Expression
    }

    public class OperationInfoViewModel
    {
        public string? Title { get; set; }
        public OperationType Type { get; set; }
        public IOperation? Operation { get; set; }
        public List<string?> Input { get; } = new List<string?>();  // 이것의 정체는 뭐냐??
        public uint MinInput { get; set; }
        public uint MaxInput { get; set; }

    }
}
