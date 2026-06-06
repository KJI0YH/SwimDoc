namespace UI.Models.Rows;

public interface IEntityRowView<TEntity> where TEntity : class
{
    TEntity Entity { get; }
}
