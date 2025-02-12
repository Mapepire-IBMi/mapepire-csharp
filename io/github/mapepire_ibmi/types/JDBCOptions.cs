using io.github.mapepire_ibmi.types.jdbcOptions;

namespace io.github.mapepire_ibmi.types {

public class JDBCOptions { 



        /**
     * The JDBC options.
     */
    private Dictionary<Object,Object> options = new Dictionary<Object,Object>();

    /**
     * Construct a new JDBCOptions instance.
     */
    public JDBCOptions() {

    }

    /**
     * Construct a new JDBCOptions instance.
     * 
     * @param options The JDBC options.
     */
    public JDBCOptions(Dictionary<Object,Object> options) {
        this.options = options;
    }

    /**
     * Get the JDBC options.
     * 
     * @return The JDBC options.
     */
    public Dictionary<Object,Object> getOptions() {
        return this.options;
    }

    /**
     * Get the value of a JDBC option.
     * 
     * @param option The JDBC option.
     * @return The value of the JDBC option.
     */
    public Object? getOption(String option) {
        return this.options.GetValueOrDefault(option);
    }

    /**
     * Set the value of a JDBC option.
     * 
     * @param option The JDBC option.
     * @param value  The value of the JDBC option.
     */
    private void SetOption(String option, Object value) {
        this.options.Add(option, value);
    }

    /**
     * Set the "naming" JDBC option.
     * 
     * @param naming The value to set.
     */
    public void SetNaming(Naming naming) {
        this.SetOption(Option.NAMING, naming.getValue());
    }

    /**
     * Set the "date format" JDBC option.
     * 
     * @param dateFormat The value to set.
     */
    public void SetDateFormat(DateFormat dateFormat) {
        this.SetOption(Option.DATE_FORMAT, dateFormat.getValue());
    }

    /**
     * Set the "date separator" JDBC option.
     * 
     * @param dateSeparator The value to set.
     */
    public void SetDateSeparator(DateSeparator dateSeparator) {
        this.SetOption(Option.DATE_SEPARATOR, dateSeparator.getValue());
    }

    /**
     * Set the "decimal separator" JDBC option.
     * 
     * @param decimalSeparator The value to set.
     */
    public void SetDecimalSeparator(DecimalSeparator decimalSeparator) {
        this.SetOption(Option.DECIMAL_SEPARATOR, decimalSeparator.getValue());
    }

    /**
     * Set the "time format" JDBC option.
     * 
     * @param timeFormat The value to set.
     */
    public void SetTimeFormat(TimeFormat timeFormat) {
        this.SetOption(Option.TIME_FORMAT, timeFormat.getValue());
    }

    /**
     * Set the "time separator" JDBC option.
     * 
     * @param timeSeparator The value to set.
     */
    public void SetTimeSeparator(TimeSeparator timeSeparator) {
        this.SetOption(Option.TIME_SEPARATOR, timeSeparator.getValue());
    }

    /**
     * Set the "full open" JDBC option.
     * 
     * @param fullOpen The value to set.
     */
    public void SetFullOpen(bool fullOpen) {
        this.SetOption(Option.FULL_OPEN, fullOpen);
    }

    /**
     * Set the "access" JDBC option.
     * 
     * @param access The value to set.
     */
    public void SetAccess(Access access) {
        this.SetOption(Option.ACCESS, access.getValue());
    }

    /**
     * Set the "autocommit exception" JDBC option.
     * 
     * @param autocommitException The value to set.
     */
    public void SetAutocommitException(String autocommitException) {
        this.SetOption(Option.AUTOCOMMIT_EXCEPTION, autocommitException);
    }

    /**
     * Set the "bidi string type" JDBC option.
     * 
     * @param bidiStringType The value to set.
     */
    public void SetBidiStringType(BidiStringType bidiStringType) {
        this.SetOption(Option.BIDI_STRING_TYPE, bidiStringType.getValue());
    }

    /**
     * Set the "bidi implicit reordering" JDBC option.
     * 
     * @param bidiImplicitReordering The value to set.
     */
    public void SetBidiImplicitReordering(bool bidiImplicitReordering) {
        this.SetOption(Option.BIDI_IMPLICIT_REORDERING, bidiImplicitReordering);
    }

    /**
     * Set the "bidi numeric ordering" JDBC option.
     * 
     * @param bidiNumericOrdering The value to set.
     */
    public void SetBidiNumericOrdering(bool bidiNumericOrdering) {
        this.SetOption(Option.BIDI_NUMERIC_ORDERING, bidiNumericOrdering);
    }

    /**
     * Set the "data truncation" JDBC option.
     * 
     * @param dataTruncation The value to set.
     */
    public void SetDataTruncation(bool dataTruncation) {
        this.SetOption(Option.DATA_TRUNCATION, dataTruncation);
    }

    /**
     * Set the "driver" JDBC option.
     * 
     * @param driver The value to set.
     */
    public void SetDriver(Driver driver) {
        this.SetOption(Option.DRIVER, driver.getValue());
    }

    /**
     * Set the "errors" JDBC option.
     * 
     * @param errors The value to set.
     */
    public void SetErrors(Errors errors) {
        this.SetOption(Option.ERRORS, errors.getValue());
    }

    /**
     * Set the "extended metadata" JDBC option.
     * 
     * @param extendedMetadata The value to set.
     */
    public void SetExtendedMetadata(bool extendedMetadata) {
        this.SetOption(Option.EXTENDED_METADATA, extendedMetadata);
    }

    /**
     * Set the "hold input locators" JDBC option.
     * 
     * @param holdInputLocators The value to set.
     */
    public void SetHoldInputLocators(bool holdInputLocators) {
        this.SetOption(Option.HOLD_INPUT_LOCATORS, holdInputLocators);
    }

    /**
     * Set the "hold statements" JDBC option.
     * 
     * @param holdStatements The value to set.
     */
    public void SetHoldStatements(bool holdStatements) {
        this.SetOption(Option.HOLD_STATEMENTS, holdStatements);
    }

    /**
     * Set the "ignore warnings" JDBC option.
     * 
     * @param ignoreWarnings The value to set.
     */
    public void SetIgnoreWarnings(String ignoreWarnings) {
        this.SetOption(Option.IGNORE_WARNINGS, ignoreWarnings);
    }

    /**
     * Set the "keep alive" JDBC option.
     * 
     * @param keepAlive The value to set.
     */
    public void SetKeepAlive(bool keepAlive) {
        this.SetOption(Option.KEEP_ALIVE, keepAlive);
    }

    /**
     * Set the "key ring name" JDBC option.
     * 
     * @param keyRingName The value to set.
     */
    public void SetKeyRingName(String keyRingName) {
        this.SetOption(Option.KEY_RING_NAME, keyRingName);
    }

    /**
     * Set the "key ring password" JDBC option.
     * 
     * @param keyRingPassword The value to set.
     */
    public void SetKeyRingPassword(String keyRingPassword) {
        this.SetOption(Option.KEY_RING_PASSWORD, keyRingPassword);
    }

    /**
     * Set the "metadata source" JDBC option.
     * 
     * @param metadataSource The value to set.
     */
    public void SetMetadataSource(MetadataSource metadataSource) {
        this.SetOption(Option.METADATA_SOURCE, metadataSource.getValue());
    }

    /**
     * Set the "proxy server" JDBC option.
     * 
     * @param proxyServer The value to set.
     */
    public void SetProxyServer(String proxyServer) {
        this.SetOption(Option.PROXY_SERVER, proxyServer);
    }

    /**
     * Set the "remarks" JDBC option.
     * 
     * @param remarks The value to set.
     */
    public void SetRemarks(Remarks remarks) {
        this.SetOption(Option.REMARKS, remarks.getValue());
    }

    /**
     * Set the "secondary URL" JDBC option.
     * 
     * @param secondaryUrl The value to set.
     */
    public void SetSecondaryUrl(bool secondaryUrl) {
        this.SetOption(Option.SECONDARY_URL, secondaryUrl);
    }

    /**
     * Set the "secure" JDBC option.
     * 
     * @param secure The value to set.
     */
    public void SetSecure(bool secure) {
        this.SetOption(Option.SECURE, secure);
    }

    /**
     * Set the "server trace" JDBC option.
     * 
     * @param serverTrace The value to set.
     */
    public void SetServerTrace(ServerTrace serverTrace) {
        this.SetOption(Option.SERVER_TRACE, serverTrace.getValue());
    }

    /**
     * Set the "thread used" JDBC option.
     * 
     * @param threadUsed The value to set.
     */
    public void SetThreadUsed(bool threadUsed) {
        this.SetOption(Option.THREAD_USED, threadUsed);
    }

    /**
     * Set the "toolbox trace" JDBC option.
     * 
     * @param toolboxTrace The value to set.
     */
    public void SetToolboxTrace(ToolboxTrace toolboxTrace) {
        this.SetOption(Option.TOOLBOX_TRACE, toolboxTrace.getValue());
    }

    /**
     * Set the "trace" JDBC option.
     * 
     * @param trace The value to set.
     */
    public void SetTrace(bool trace) {
        this.SetOption(Option.TRACE, trace);
    }

    /**
     * Set the "translate binary" JDBC option.
     * 
     * @param translateBinary The value to set.
     */
    public void SetTranslateBinary(bool translateBinary) {
        this.SetOption(Option.TRANSLATE_BINARY, translateBinary);
    }

    /**
     * Set the "translate bool" JDBC option.
     * 
     * @param translateBoolean The value to set.
     */
    public void SetTranslateBoolean(bool translateBoolean) {
        this.SetOption(Option.TRANSLATE_BOOLEAN, translateBoolean);
    }

    /**
     * Set the "libraries" JDBC option.
     * 
     * @param libraries The value to set.
     */
    public void SetLibraries(List<String> libraries) {
        this.SetOption(Option.LIBRARIES, libraries);
    }

    /**
     * Set the "auto commit" JDBC option.
     * 
     * @param autoCommit The value to set.
     */
    public void SetAutoCommit(bool autoCommit) {
        this.SetOption(Option.AUTO_COMMIT, autoCommit);
    }

    /**
     * Set the "concurrent access resolution" JDBC option.
     * 
     * @param concurrentAccessResolution The value to set.
     */
    public void SetConcurrentAccessResolution(ConcurrentAccessResolution concurrentAccessResolution) {
        this.SetOption(Option.CONCURRENT_ACCESS_RESOLUTION, concurrentAccessResolution.getValue());
    }

    /**
     * Set the "cursor hold" JDBC option.
     * 
     * @param cursorHold The value to set.
     */
    public void SetCursorHold(bool cursorHold) {
        this.SetOption(Option.CURSOR_HOLD, cursorHold);
    }

    /**
     * Set the "cursor sensitivity" JDBC option.
     * 
     * @param cursorSensitivity The value to set.
     */
    public void SetCursorSensitivity(CursorSensitivity cursorSensitivity) {
        this.SetOption(Option.CURSOR_SENSITIVITY, cursorSensitivity.getValue());
    }

    /**
     * Set the "database name" JDBC option.
     * 
     * @param databaseName The value to set.
     */
    public void SetDatabaseName(String databaseName) {
        this.SetOption(Option.DATABASE_NAME, databaseName);
    }

    /**
     * Set the "decfloat rounding mode" JDBC option.
     * 
     * @param decfloatRoundingMode The value to set.
     */
    public void SetDecfloatRoundingMode(DecfloatRoundingMode decfloatRoundingMode) {
        this.SetOption(Option.DECFLOAT_ROUNDING_MODE, decfloatRoundingMode.getValue());
    }

    /**
     * Set the "maximum precision" JDBC option.
     * 
     * @param maximumPrecision The value to set.
     */
    public void SetMaximumPrecision(MaximumPrecision maximumPrecision) {
        this.SetOption(Option.MAXIMUM_PRECISION, maximumPrecision.getValue());
    }

    /**
     * Set the "maximum scale" JDBC option.
     * 
     * @param maximumScale The value to set.
     */
    public void SetMaximumScale(String maximumScale) {
        this.SetOption(Option.MAXIMUM_SCALE, maximumScale);
    }

    /**
     * Set the "minimum divide scale" JDBC option.
     * 
     * @param minimumDivideScale The value to set.
     */
    public void SetMinimumDivideScale(MinimumDivideScale minimumDivideScale) {
        this.SetOption(Option.MINIMUM_DIVIDE_SCALE, minimumDivideScale.getValue());
    }

    /**
     * Set the "package ccsid" JDBC option.
     * 
     * @param packageCcsid The value to set.
     */
    public void SetPackageCcsid(PackageCcsid packageCcsid) {
        this.SetOption(Option.PACKAGE_CCSID, packageCcsid.getValue());
    }

    /**
     * Set the "transaction isolation" JDBC option.
     * 
     * @param transactionIsolation The value to set.
     */
    public void SetTransactionIsolation(TransactionIsolation transactionIsolation) {
        this.SetOption(Option.TRANSACTION_ISOLATION, transactionIsolation.getValue());
    }

    /**
     * Set the "translate hex" JDBC option.
     * 
     * @param translateHex The value to set.
     */
    public void SetTranslateHex(TranslateHex translateHex) {
        this.SetOption(Option.TRANSLATE_HEX, translateHex.getValue());
    }

    /**
     * Set the "true autocommit" JDBC option.
     * 
     * @param trueAutocommit The value to set.
     */
    public void SetTrueAutocommit(bool trueAutocommit) {
        this.SetOption(Option.TRUE_AUTOCOMMIT, trueAutocommit);
    }

    /**
     * Set the "xa loosely coupled support" JDBC option.
     * 
     * @param xaLooselyCoupledSupport The value to set.
     */
    public void SetXALooselyCoupledSupport(XALooselyCoupledSupport xaLooselyCoupledSupport) {
        this.SetOption(Option.XA_LOOSELY_COUPLED_SUPPORT, xaLooselyCoupledSupport.getValue());
    }

    /**
     * Set the "big decimal" JDBC option.
     * 
     * @param bigDecimal The value to set.
     */
    public void SetBigDecimal(bool bigDecimal) {
        this.SetOption(Option.BIG_DECIMAL, bigDecimal);
    }

    /**
     * Set the "block criteria" JDBC option.
     * 
     * @param blockCriteria The value to set.
     */
    public void SetBlockCriteria(BlockCriteria blockCriteria) {
        this.SetOption(Option.BLOCK_CRITERIA, blockCriteria.getValue());
    }

    /**
     * Set the "block size" JDBC option.
     * 
     * @param blockSize The value to set.
     */
    public void SetBlockSize(BlockSize blockSize) {
        this.SetOption(Option.BLOCK_SIZE, blockSize.getValue());
    }

    /**
     * Set the "data compression" JDBC option.
     * 
     * @param dataCompression The value to set.
     */
    public void SetDataCompression(bool dataCompression) {
        this.SetOption(Option.DATA_COMPRESSION, dataCompression);
    }

    /**
     * Set the "extended dynamic" JDBC option.
     * 
     * @param extendedDynamic The value to set.
     */
    public void SetExtendedDynamic(bool extendedDynamic) {
        this.SetOption(Option.EXTENDED_DYNAMIC, extendedDynamic);
    }

    /**
     * Set the "lazy close" JDBC option.
     * 
     * @param lazyClose The value to set.
     */
    public void SetLazyClose(bool lazyClose) {
        this.SetOption(Option.LAZY_CLOSE, lazyClose);
    }

    /**
     * Set the "lob threshold" JDBC option.
     * 
     * @param lobThreshold The value to set.
     */
    public void SetLobThreshold(String lobThreshold) {
        this.SetOption(Option.LOB_THRESHOLD, lobThreshold);
    }

    /**
     * Set the "maximum blocked input rows" JDBC option.
     * 
     * @param maximumBlockedInputRows The value to set.
     */
    public void SetMaximumBlockedInputRows(String maximumBlockedInputRows) {
        this.SetOption(Option.MAXIMUM_BLOCKED_INPUT_ROWS, maximumBlockedInputRows);
    }

    /**
     * Set the "package" JDBC option.
     * 
     * @param pkg The value to set.
     */
    public void SetPackage(String pkg) {
        this.SetOption(Option.PACKAGE, pkg);
    }

    /**
     * Set the "package add" JDBC option.
     * 
     * @param packageAdd The value to set.
     */
    public void SetPackageAdd(bool packageAdd) {
        this.SetOption(Option.PACKAGE_ADD, packageAdd);
    }

    /**
     * Set the "package cache" JDBC option.
     * 
     * @param packageCache The value to set.
     */
    public void SetPackageCache(bool packageCache) {
        this.SetOption(Option.PACKAGE_CACHE, packageCache);
    }

    /**
     * Set the "package criteria" JDBC option.
     * 
     * @param packageCriteria The value to set.
     */
    public void SetPackageCriteria(PackageCriteria packageCriteria) {
        this.SetOption(Option.PACKAGE_CRITERIA, packageCriteria.getValue());
    }

    /**
     * Set the "package error" JDBC option.
     * 
     * @param packageError The value to set.
     */
    public void SetPackageError(PackageError packageError) {
        this.SetOption(Option.PACKAGE_ERROR, packageError.getValue());
    }

    /**
     * Set the "package library" JDBC option.
     * 
     * @param packageLibrary The value to set.
     */
    public void SetPackageLibrary(String packageLibrary) {
        this.SetOption(Option.PACKAGE_LIBRARY, packageLibrary);
    }

    /**
     * Set the "prefetch" JDBC option.
     * 
     * @param prefetch The value to set.
     */
    public void SetPrefetch(bool prefetch) {
        this.SetOption(Option.PREFETCH, prefetch);
    }

    /**
     * Set the "qaqqinilib" JDBC option.
     * 
     * @param qaqqinilib The value to set.
     */
    public void SetQaqqinilib(String qaqqinilib) {
        this.SetOption(Option.QAQQINILIB, qaqqinilib);
    }

    /**
     * Set the "query optimize goal" JDBC option.
     * 
     * @param queryOptimizeGoal The value to set.
     */
    public void SetQueryOptimizeGoal(QueryOptimizeGoal queryOptimizeGoal) {
        this.SetOption(Option.QUERY_OPTIMIZE_GOAL, queryOptimizeGoal.getValue());
    }

    /**
     * Set the "query timeout mechanism" JDBC option.
     * 
     * @param queryTimeoutMechanism The value to set.
     */
    public void SetQueryTimeoutMechanism(QueryTimeoutMechanism queryTimeoutMechanism) {
        this.SetOption(Option.QUERY_TIMEOUT_MECHANISM, queryTimeoutMechanism.getValue());
    }

    /**
     * Set the "query storage limit" JDBC option.
     * 
     * @param queryStorageLimit The value to set.
     */
    public void SetQueryStorageLimit(String queryStorageLimit) {
        this.SetOption(Option.QUERY_STORAGE_LIMIT, queryStorageLimit);
    }

    /**
     * Set the "receive buffer size" JDBC option.
     * 
     * @param receiveBufferSize The value to set.
     */
    public void receiveBufferSize(String receiveBufferSize) {
        this.SetOption(Option.RECEIVE_BUFFER_SIZE, receiveBufferSize);
    }

    /**
     * Set the "send buffer size" JDBC option.
     * 
     * @param sendBufferSize The value to set.
     */
    public void sendBufferSize(String sendBufferSize) {
        this.SetOption(Option.SEND_BUFFER_SIZE, sendBufferSize);
    }

    /**
     * Set the "variable field compression" JDBC option.
     * 
     * @param variableFieldCompression The value to set.
     */
    public void variableFieldCompression(bool variableFieldCompression) {
        this.SetOption(Option.VARIABLE_FIELD_COMPRESSION, variableFieldCompression);
    }

    /**
     * Set the "sort" JDBC option.
     * 
     * @param sort The value to set.
     */
    public void SetSort(Sort sort) {
        this.SetOption(Option.SORT, sort);
    }

    /**
     * Set the "sort language" JDBC option.
     * 
     * @param sortLanguage The value to set.
     */
    public void sortLanguage(String sortLanguage) {
        this.SetOption(Option.SORT_LANGUAGE, sortLanguage);
    }

    /**
     * Set the "sort table" JDBC option.
     * 
     * @param sortTable The value to set.
     */
    public void sortTable(String sortTable) {
        this.SetOption(Option.SORT_TABLE, sortTable);
    }

    /**
     * Set the "sort weight" JDBC option.
     * 
     * @param sortWeight The value to set.
     */
    public void SetWeight(SortWeight sortWeight) {
        this.SetOption(Option.SORT_WEIGHT, sortWeight);
    }

}



}