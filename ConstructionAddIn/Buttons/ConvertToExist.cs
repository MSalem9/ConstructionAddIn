using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

using System;
using System.Linq;

namespace ConstructionAddIn.Buttons
{
    internal class ConvertToExist : Button
    {
        protected override async void OnClick()
        {
            // ============================================================
            // SETTINGS - CHANGE THESE TWO VALUES
            // ============================================================
            const string LAYER_NAME = "pipes";
            const string FIELD_NAME = "Details";
            // ============================================================

            if (MapView.Active == null)
            {
                MessageBox.Show(
                    "No active map is open.",
                    "Convert To Exist");

                return;
            }

            try
            {
                string resultMessage = await QueuedTask.Run(() =>
                {
                    // ----------------------------------------------------
                    // 1. Find layer
                    // ----------------------------------------------------
                    FeatureLayer layer = MapView.Active.Map
                        .GetLayersAsFlattenedList()
                        .OfType<FeatureLayer>()
                        .FirstOrDefault(l =>
                            string.Equals(
                                l.Name,
                                LAYER_NAME,
                                StringComparison.OrdinalIgnoreCase));

                    if (layer == null)
                    {
                        return $"Layer '{LAYER_NAME}' was not found.";
                    }

                    // ----------------------------------------------------
                    // 2. Check field exists
                    // ----------------------------------------------------
                    string actualFieldName;

                    using (Table table = layer.GetTable())
                    using (TableDefinition definition = table.GetDefinition())
                    {
                        Field field = definition
                            .GetFields()
                            .FirstOrDefault(f =>
                                string.Equals(
                                    f.Name,
                                    FIELD_NAME,
                                    StringComparison.OrdinalIgnoreCase));

                        if (field == null)
                        {
                            return $"Field '{FIELD_NAME}' was not found " +
                                   $"in layer '{LAYER_NAME}'.";
                        }

                        // Make sure field is a text field
                        if (field.FieldType != FieldType.String)
                        {
                            return $"Field '{FIELD_NAME}' is not a text field.";
                        }

                        actualFieldName = field.Name;
                    }

                    // ----------------------------------------------------
                    // 3. Get selected features
                    // ----------------------------------------------------
                    using Selection selection = layer.GetSelection();

                    var selectedOIDs = selection.GetObjectIDs();

                    if (selectedOIDs.Count == 0)
                    {
                        return $"No features are selected in '{LAYER_NAME}'.";
                    }

                    // ----------------------------------------------------
                    // 4. Create edit operation
                    // ----------------------------------------------------
                    EditOperation editOperation = new EditOperation
                    {
                        Name = "Convert To Exist"
                    };

                    int changedCount = 0;
                    int alreadyExistCount = 0;

                    // ----------------------------------------------------
                    // 5. Loop through selected features
                    // ----------------------------------------------------
                    foreach (long oid in selectedOIDs)
                    {
                        Inspector inspector = new Inspector();

                        inspector.Load(layer, oid);

                        object value = inspector[actualFieldName];

                        string currentText =
                            value == null || value == DBNull.Value
                                ? ""
                                : value.ToString();

                        currentText = currentText.Trim();

                        // ------------------------------------------------
                        // Already valid:
                        //
                        // Exist
                        //
                        // or
                        //
                        // Exist - 8'' CS API-5L GR.B SCH.80 PE-COATED
                        // ------------------------------------------------
                        bool alreadyExist =
                            currentText.Equals(
                                "Exist",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            currentText.StartsWith(
                                "Exist - ",
                                StringComparison.OrdinalIgnoreCase);

                        if (alreadyExist)
                        {
                            alreadyExistCount++;
                            continue;
                        }

                        // ------------------------------------------------
                        // Build new value
                        // ------------------------------------------------
                        string newText;

                        if (string.IsNullOrWhiteSpace(currentText))
                        {
                            newText = "Exist";
                        }
                        else
                        {
                            newText = "Exist - " + currentText;
                        }

                        // ------------------------------------------------
                        // Update inspector
                        // ------------------------------------------------
                        inspector[actualFieldName] = newText;

                        // Add update to edit operation
                        editOperation.Modify(inspector);

                        changedCount++;
                    }

                    // ----------------------------------------------------
                    // 6. Nothing needed changing
                    // ----------------------------------------------------
                    if (changedCount == 0)
                    {
                        return
                            $"No changes were needed.\n\n" +
                            $"{alreadyExistCount} selected feature(s) " +
                            $"already contain 'Exist'.";
                    }

                    // ----------------------------------------------------
                    // 7. Execute edit
                    // ----------------------------------------------------
                    bool success = editOperation.Execute();

                    if (!success)
                    {
                        return
                            "Edit operation failed.\n\n" +
                            editOperation.ErrorMessage;
                    }

                    // ----------------------------------------------------
                    // 8. Return result
                    // ----------------------------------------------------
                    return
                        $"Completed successfully.\n\n" +
                        $"Updated: {changedCount}\n" +
                        $"Already Exist: {alreadyExistCount}";
                });

                MessageBox.Show(
                    resultMessage,
                    "Convert To Exist");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred:\n\n{ex.Message}",
                    "Convert To Exist");
            }
        }
    }
}