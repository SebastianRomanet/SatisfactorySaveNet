using System.Collections.Generic;
namespace SatisfactorySaveNet.Abstracts.Model.Properties;

public class ArrayFINNetworkProperty : ArrayPropertyBase
{
    public override ArrayPropertyConstraint ArrayValueType => ArrayPropertyConstraint.FINNetwork;

    public ICollection<FINNetworkProperty> Values { get; set; } = [];
}

