using International.EInvoicing;
using International.EInvoicing.Model;
using International.EInvoicing.Samples;
using International.EInvoicing.Samples.Chapters;

Console.WriteLine("International.EInvoicing — everything the library does, in one run.");
Console.WriteLine("Each chapter is a thing you might arrive wanting to do. The code is the point; the output");
Console.WriteLine("is here so you can see it happen. Source: samples/International.EInvoicing.Samples.");

EInvoicing einvoicing = Wiring.Assemble();
Wiring.ThroughAContainer();

EInvoice invoice = Invoices.Build();
string ubl = Invoices.Write(einvoicing, invoice);
Invoices.ReadBack(einvoicing, ubl);
Invoices.Validate(einvoicing, ubl);

HostileDocuments.Run(einvoicing);
Extending.Run();
HybridPdf.Run(Invoices.Build(announce: false));

FrenchLifecycle.Run(einvoicing);
FrenchEReporting.Run();
NationalIdentifiers.Run();
NationalRuleSets.Run();

Console.WriteLine();
Console.WriteLine("Done. Every chapter above is a page in docs/guides — start at getting-started.md.");
