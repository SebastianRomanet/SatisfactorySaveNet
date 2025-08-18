using System.Collections.Generic;
namespace SatisfactorySaveNet.Abstracts.Model.Properties;

public class ArrayInt8Property : ArrayPropertyBase
{
    public override ArrayPropertyConstraint ArrayValueType => ArrayPropertyConstraint.Int8;

    public ICollection<sbyte> Values { get; set; } = [];
}

