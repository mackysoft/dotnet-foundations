# Test vector provenance

`Rfc8785OfficialVectorTests` fixes expected bytes independently from the implementation under test.

- The primitive serialization example comes from [RFC 8785 Sections 3.2.2 through 3.2.4](https://www.rfc-editor.org/rfc/rfc8785.html#section-3.2.2).
- The finite IEEE 754 cases come from [RFC 8785 Appendix B](https://www.rfc-editor.org/rfc/rfc8785.html#appendix-B). The negative-zero row is covered as a rejection case in accordance with [verified erratum 7920](https://www.rfc-editor.org/errata/eid7920). The non-finite rows are invalid JSON inputs.
- The `arrays`, `french`, `structures`, `unicode`, `values`, and `weird` input/output pairs come from the [`testdata` directory at commit `19d51d7fe467d4706a3ff08adf8a748f29fc21e0`](https://github.com/cyberphone/json-canonicalization/tree/19d51d7fe467d4706a3ff08adf8a748f29fc21e0/testdata), the repository referenced by RFC 8785 Appendix I.

The official output pairs are retained as hexadecimal UTF-8 bytes in the test source. Updating them requires checking the RFC or the pinned upstream commit rather than generating values with this package.

See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for the copyright and license terms that apply to the upstream test data.
