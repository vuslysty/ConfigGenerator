using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public interface ITableCandidateParser
{
    bool TryParse(TableParsingContext context, out TableData? tableData);
}
