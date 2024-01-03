using System;
using Microsoft.CodeAnalysis;

namespace NationalInstruments.Analyzers.Utilities.Extensions
{
    public static class ReportDiagnosticExtensions
    {
        public static DiagnosticSeverity? ToDiagnosticSeverity(this ReportDiagnostic reportDiagnostic)
        {
            return reportDiagnostic switch
            {
                ReportDiagnostic.Error => DiagnosticSeverity.Error,
                ReportDiagnostic.Warn => DiagnosticSeverity.Warning,
                ReportDiagnostic.Info => DiagnosticSeverity.Info,
                ReportDiagnostic.Hidden => DiagnosticSeverity.Hidden,
                ReportDiagnostic.Suppress => null,
                ReportDiagnostic.Default => null,
                _ => throw new NotImplementedException(),
            };
        }

        public static bool IsLessSevereThan(this ReportDiagnostic current, ReportDiagnostic other)
        {
            return current switch
            {
                ReportDiagnostic.Error => false,

                ReportDiagnostic.Warn =>
                    other switch
                    {
                        ReportDiagnostic.Error => true,
                        _ => false
                    },

                ReportDiagnostic.Info =>
                    other switch
                    {
                        ReportDiagnostic.Error => true,
                        ReportDiagnostic.Warn => true,
                        _ => false
                    },

                ReportDiagnostic.Hidden =>
                    other switch
                    {
                        ReportDiagnostic.Error => true,
                        ReportDiagnostic.Warn => true,
                        ReportDiagnostic.Info => true,
                        _ => false
                    },

                ReportDiagnostic.Suppress => true,

                _ => false
            };
        }
    }
}
