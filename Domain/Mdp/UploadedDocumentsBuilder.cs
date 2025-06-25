using System;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.Journeys;

namespace WTW.MdpService.Test.Domain.Mdp;

public class UploadedDocumentsBuilder
{
    public string _referenceNumber = "TestReferenceNumber";
    public string _businessGroup = "TestBusinessGroup";
    public string _journeyType = "transfer2";
    public string _documentType = "TestDocumentType";
    public string _fileName = "TestName.pdf";
    public string _uuid = Guid.NewGuid().ToString();
    public bool _isEdoc = false;
    public string _tags = "TAG1";
    public DocumentSource _documentSource = DocumentSource.Outgoing;

    public UploadedDocument Build()
    {
        return new UploadedDocument(_referenceNumber, _businessGroup, _journeyType, _documentType, _fileName, _uuid, _documentSource, _isEdoc, _tags);
    }

    public UploadedDocumentsBuilder Type(string type)
    {
        _journeyType = type;
        return this;
    }

    public UploadedDocumentsBuilder FileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public UploadedDocumentsBuilder Tags(string tags)
    {
        _tags = tags;
        return this;
    }

    public UploadedDocumentsBuilder Uuid(string uuid)
    {
        _uuid = uuid;
        return this;
    }

    public UploadedDocumentsBuilder DocumentType(string documentType)
    {
        _documentType = documentType;
        return this;
    }
}