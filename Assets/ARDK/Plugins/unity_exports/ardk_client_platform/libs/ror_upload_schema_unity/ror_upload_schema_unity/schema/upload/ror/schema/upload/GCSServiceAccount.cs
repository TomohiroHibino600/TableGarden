// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace ror.schema.upload
{

using global::System;
using global::System.Collections.Generic;
using global::FlatBuffers;

public struct GCSServiceAccount : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_1_12_0(); }
  public static GCSServiceAccount GetRootAsGCSServiceAccount(ByteBuffer _bb) { return GetRootAsGCSServiceAccount(_bb, new GCSServiceAccount()); }
  public static GCSServiceAccount GetRootAsGCSServiceAccount(ByteBuffer _bb, GCSServiceAccount obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public GCSServiceAccount __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public string ClientEmail { get { int o = __p.__offset(4); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetClientEmailBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
  public ArraySegment<byte>? GetClientEmailBytes() { return __p.__vector_as_arraysegment(4); }
#endif
  public byte[] GetClientEmailArray() { return __p.__vector_as_array<byte>(4); }
  public string PrivateKeyID { get { int o = __p.__offset(6); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetPrivateKeyIDBytes() { return __p.__vector_as_span<byte>(6, 1); }
#else
  public ArraySegment<byte>? GetPrivateKeyIDBytes() { return __p.__vector_as_arraysegment(6); }
#endif
  public byte[] GetPrivateKeyIDArray() { return __p.__vector_as_array<byte>(6); }
  public string PrivateKey { get { int o = __p.__offset(8); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetPrivateKeyBytes() { return __p.__vector_as_span<byte>(8, 1); }
#else
  public ArraySegment<byte>? GetPrivateKeyBytes() { return __p.__vector_as_arraysegment(8); }
#endif
  public byte[] GetPrivateKeyArray() { return __p.__vector_as_array<byte>(8); }
  public string TokenURI { get { int o = __p.__offset(10); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetTokenURIBytes() { return __p.__vector_as_span<byte>(10, 1); }
#else
  public ArraySegment<byte>? GetTokenURIBytes() { return __p.__vector_as_arraysegment(10); }
#endif
  public byte[] GetTokenURIArray() { return __p.__vector_as_array<byte>(10); }
  public string Scopes(int j) { int o = __p.__offset(12); return o != 0 ? __p.__string(__p.__vector(o) + j * 4) : null; }
  public int ScopesLength { get { int o = __p.__offset(12); return o != 0 ? __p.__vector_len(o) : 0; } }
  public string Subject { get { int o = __p.__offset(14); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetSubjectBytes() { return __p.__vector_as_span<byte>(14, 1); }
#else
  public ArraySegment<byte>? GetSubjectBytes() { return __p.__vector_as_arraysegment(14); }
#endif
  public byte[] GetSubjectArray() { return __p.__vector_as_array<byte>(14); }

  public static Offset<ror.schema.upload.GCSServiceAccount> CreateGCSServiceAccount(FlatBufferBuilder builder,
      StringOffset clientEmailOffset = default(StringOffset),
      StringOffset privateKeyIDOffset = default(StringOffset),
      StringOffset privateKeyOffset = default(StringOffset),
      StringOffset tokenURIOffset = default(StringOffset),
      VectorOffset scopesOffset = default(VectorOffset),
      StringOffset subjectOffset = default(StringOffset)) {
    builder.StartTable(6);
    GCSServiceAccount.AddSubject(builder, subjectOffset);
    GCSServiceAccount.AddScopes(builder, scopesOffset);
    GCSServiceAccount.AddTokenURI(builder, tokenURIOffset);
    GCSServiceAccount.AddPrivateKey(builder, privateKeyOffset);
    GCSServiceAccount.AddPrivateKeyID(builder, privateKeyIDOffset);
    GCSServiceAccount.AddClientEmail(builder, clientEmailOffset);
    return GCSServiceAccount.EndGCSServiceAccount(builder);
  }

  public static void StartGCSServiceAccount(FlatBufferBuilder builder) { builder.StartTable(6); }
  public static void AddClientEmail(FlatBufferBuilder builder, StringOffset clientEmailOffset) { builder.AddOffset(0, clientEmailOffset.Value, 0); }
  public static void AddPrivateKeyID(FlatBufferBuilder builder, StringOffset privateKeyIDOffset) { builder.AddOffset(1, privateKeyIDOffset.Value, 0); }
  public static void AddPrivateKey(FlatBufferBuilder builder, StringOffset privateKeyOffset) { builder.AddOffset(2, privateKeyOffset.Value, 0); }
  public static void AddTokenURI(FlatBufferBuilder builder, StringOffset tokenURIOffset) { builder.AddOffset(3, tokenURIOffset.Value, 0); }
  public static void AddScopes(FlatBufferBuilder builder, VectorOffset scopesOffset) { builder.AddOffset(4, scopesOffset.Value, 0); }
  public static VectorOffset CreateScopesVector(FlatBufferBuilder builder, StringOffset[] data) { builder.StartVector(4, data.Length, 4); for (int i = data.Length - 1; i >= 0; i--) builder.AddOffset(data[i].Value); return builder.EndVector(); }
  public static VectorOffset CreateScopesVectorBlock(FlatBufferBuilder builder, StringOffset[] data) { builder.StartVector(4, data.Length, 4); builder.Add(data); return builder.EndVector(); }
  public static void StartScopesVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(4, numElems, 4); }
  public static void AddSubject(FlatBufferBuilder builder, StringOffset subjectOffset) { builder.AddOffset(5, subjectOffset.Value, 0); }
  public static Offset<ror.schema.upload.GCSServiceAccount> EndGCSServiceAccount(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    builder.Required(o, 4);  // clientEmail
    builder.Required(o, 6);  // privateKeyID
    builder.Required(o, 8);  // privateKey
    builder.Required(o, 10);  // tokenURI
    return new Offset<ror.schema.upload.GCSServiceAccount>(o);
  }
  public GCSServiceAccountT UnPack() {
    var _o = new GCSServiceAccountT();
    this.UnPackTo(_o);
    return _o;
  }
  public void UnPackTo(GCSServiceAccountT _o) {
    _o.ClientEmail = this.ClientEmail;
    _o.PrivateKeyID = this.PrivateKeyID;
    _o.PrivateKey = this.PrivateKey;
    _o.TokenURI = this.TokenURI;
    _o.Scopes = new List<string>();
    for (var _j = 0; _j < this.ScopesLength; ++_j) {_o.Scopes.Add(this.Scopes(_j));}
    _o.Subject = this.Subject;
  }
  public static Offset<ror.schema.upload.GCSServiceAccount> Pack(FlatBufferBuilder builder, GCSServiceAccountT _o) {
    if (_o == null) return default(Offset<ror.schema.upload.GCSServiceAccount>);
    var _clientEmail = _o.ClientEmail == null ? default(StringOffset) : builder.CreateString(_o.ClientEmail);
    var _privateKeyID = _o.PrivateKeyID == null ? default(StringOffset) : builder.CreateString(_o.PrivateKeyID);
    var _privateKey = _o.PrivateKey == null ? default(StringOffset) : builder.CreateString(_o.PrivateKey);
    var _tokenURI = _o.TokenURI == null ? default(StringOffset) : builder.CreateString(_o.TokenURI);
    var _scopes = default(VectorOffset);
    if (_o.Scopes != null) {
      var __scopes = new StringOffset[_o.Scopes.Count];
      for (var _j = 0; _j < __scopes.Length; ++_j) { __scopes[_j] = builder.CreateString(_o.Scopes[_j]); }
      _scopes = CreateScopesVector(builder, __scopes);
    }
    var _subject = _o.Subject == null ? default(StringOffset) : builder.CreateString(_o.Subject);
    return CreateGCSServiceAccount(
      builder,
      _clientEmail,
      _privateKeyID,
      _privateKey,
      _tokenURI,
      _scopes,
      _subject);
  }
};

public class GCSServiceAccountT
{
  public string ClientEmail { get; set; }
  public string PrivateKeyID { get; set; }
  public string PrivateKey { get; set; }
  public string TokenURI { get; set; }
  public List<string> Scopes { get; set; }
  public string Subject { get; set; }

  public GCSServiceAccountT() {
    this.ClientEmail = null;
    this.PrivateKeyID = null;
    this.PrivateKey = null;
    this.TokenURI = null;
    this.Scopes = null;
    this.Subject = null;
  }
}


}
