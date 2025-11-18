# React + Axios 文件上传示例

## ⚠️ 重要提示：文件路径映射

在使用 `fileFacetMapping` 时，需要使用**文件路径**（而非仅文件名）来唯一标识文件，以避免同名文件冲突。

### 1. 文件路径获取

-   **推荐方式**：使用文件夹选择器（`<input type="file" webkitdirectory>`），浏览器会自动提供 `webkitRelativePath` 属性
-   **格式示例**：`folder1/subfolder/file.pdf` 或 `file.pdf`（根目录文件）
-   **优势**：支持文件夹结构，通过完整路径唯一标识文件，避免同名文件冲突

### 2. 文件路径格式

-   **相对路径**：相对于选择的根文件夹
-   **路径分隔符**：使用 `/`（正斜杠），浏览器会自动统一格式
-   **示例**：
    -   根目录文件：`document.pdf`
    -   一级子文件夹：`folder1/document.pdf`
    -   多级子文件夹：`folder1/subfolder/document.pdf`

### 3. 后端匹配逻辑

**重要**：后端 `IFormFile.FileName` **只包含文件名，不包含路径**（浏览器出于安全考虑不会发送路径）。

匹配优先级：

1. **文件索引匹配**（推荐，最可靠）：前端提供 `fileIndex`，后端按文件顺序匹配
2. **文件名+大小组合匹配**（备选方案）：如果未提供索引，使用 `fileName + fileSize` 组合匹配

**推荐做法**：

-   ✅ 始终提供 `fileIndex`（文件在数组中的索引位置，从 0 开始）
-   ✅ 同时提供 `fileName` 和 `fileSize` 作为备选匹配方式
-   ✅ 使用 `filePath` 用于前端标识和显示

### 4. 最佳实践

-   ✅ 使用文件夹选择器获取完整的文件路径信息
-   ✅ 优先使用 `webkitRelativePath` 作为 `filePath`
-   ✅ 如果无法获取路径，使用文件名（仅适用于文件名唯一的情况）
-   ✅ 确保 `filePath` 与浏览器提供的路径格式一致

## 完整示例代码（支持一个动态分面包含多个文件）

```tsx
import React, { useState } from 'react';
import axios from 'axios';

interface DynamicFacetInfo {
    catalogueName: string;
    description?: string;
    sequenceNumber?: number;
    tags?: string[];
    metadata?: Record<string, any>;
}

interface FileWithFacet {
    file: File;
    dynamicFacetKey?: string | null; // 关联到动态分面的key（catalogueName）
}

interface DynamicFacetGroup {
    key: string; // 唯一标识
    info: DynamicFacetInfo;
    fileIndices: number[]; // 属于该动态分面的文件索引
}

const SmartClassificationUpload: React.FC = () => {
    const [files, setFiles] = useState<File[]>([]);
    const [dynamicFacets, setDynamicFacets] = useState<DynamicFacetGroup[]>([]);
    const [fileFacetMapping, setFileFacetMapping] = useState<
        Map<number, string>
    >(new Map()); // 文件索引 -> 动态分面key
    const [catalogueId, setCatalogueId] = useState<string>('');
    const [loading, setLoading] = useState(false);
    const [results, setResults] = useState<any[]>([]);

    // 处理文件选择（支持文件夹选择）
    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const selectedFiles = Array.from(event.target.files || []);
        setFiles((prev) => [...prev, ...selectedFiles]);
    };

    // 创建新的动态分面
    const createDynamicFacet = () => {
        const key = `facet_${Date.now()}_${Math.random()
            .toString(36)
            .substr(2, 9)}`;
        const newFacet: DynamicFacetGroup = {
            key,
            info: {
                catalogueName: '',
                description: '',
            },
            fileIndices: [],
        };
        setDynamicFacets((prev) => [...prev, newFacet]);
        return key;
    };

    // 删除动态分面
    const deleteDynamicFacet = (key: string) => {
        setDynamicFacets((prev) => prev.filter((f) => f.key !== key));
        // 清除文件关联
        setFileFacetMapping((prev) => {
            const newMap = new Map(prev);
            for (const [fileIndex, facetKey] of newMap.entries()) {
                if (facetKey === key) {
                    newMap.delete(fileIndex);
                }
            }
            return newMap;
        });
    };

    // 更新动态分面信息
    const updateDynamicFacet = (
        key: string,
        info: Partial<DynamicFacetInfo>
    ) => {
        setDynamicFacets((prev) =>
            prev.map((f) =>
                f.key === key ? { ...f, info: { ...f.info, ...info } } : f
            )
        );
    };

    // 将文件分配到动态分面
    const assignFileToFacet = (fileIndex: number, facetKey: string | null) => {
        // 先更新文件映射
        setFileFacetMapping((prev) => {
            const newMap = new Map(prev);
            if (facetKey) {
                newMap.set(fileIndex, facetKey);
            } else {
                newMap.delete(fileIndex);
            }
            return newMap;
        });

        // 然后更新动态分面的文件索引列表
        setDynamicFacets((prevFacets) =>
            prevFacets.map((f) => {
                if (facetKey && f.key === facetKey) {
                    // 添加到目标动态分面
                    if (!f.fileIndices.includes(fileIndex)) {
                        return {
                            ...f,
                            fileIndices: [...f.fileIndices, fileIndex],
                        };
                    }
                } else {
                    // 从其他动态分面中移除
                    return {
                        ...f,
                        fileIndices: f.fileIndices.filter(
                            (idx) => idx !== fileIndex
                        ),
                    };
                }
                return f;
            })
        );
    };

    // 上传文件
    const handleUpload = async () => {
        if (!catalogueId) {
            alert('请先选择分类ID');
            return;
        }

        if (files.length === 0) {
            alert('请选择要上传的文件');
            return;
        }

        // 验证动态分面信息
        for (const facet of dynamicFacets) {
            if (facet.fileIndices.length > 0 && !facet.info.catalogueName) {
                alert(`动态分面"${facet.key}"的分类名称不能为空`);
                return;
            }
        }

        setLoading(true);
        try {
            // 使用multipart/form-data方式上传
            const uploadFormData = new FormData();

            // 添加文件
            files.forEach((file) => {
                uploadFormData.append('files', file);
            });

            // 方式1：使用fileFacetMapping明确文件与动态分面的映射关系（推荐）
            // 使用数组格式，包含文件索引和动态分面分类名称，避免文件名重复问题
            // 格式：[{"fileName":"file.pdf","fileIndex":0,"fileSize":1024,"dynamicFacetCatalogueName":"案卷1"},...]
            //
            // 优势：
            // 1. 通过文件索引唯一标识文件，最可靠的匹配方式
            // 2. 避免同名文件冲突（不同文件夹中的同名文件）
            // 3. 支持文件名+大小组合作为备选匹配方式

            interface FileFacetMappingItem {
                fileName: string; // 文件名（用于后端匹配，因为后端只能获取文件名）
                fileIndex: number; // 文件索引（必需，最可靠的匹配方式）
                fileSize?: number; // 文件大小（可选，用于辅助匹配）
                dynamicFacetCatalogueName: string; // 动态分面分类名称
            }

            const fileFacetMappingList: FileFacetMappingItem[] = [];
            files.forEach((file, index) => {
                const facetKey = fileFacetMapping.get(index);
                if (facetKey) {
                    const facet = dynamicFacets.find((f) => f.key === facetKey);
                    if (facet && facet.info.catalogueName) {
                        fileFacetMappingList.push({
                            fileName: file.name, // 文件名用于后端匹配（后端只能获取文件名）
                            fileIndex: index, // 文件索引（必需，最可靠的匹配方式，避免同名文件冲突）
                            fileSize: file.size, // 文件大小（可选，用于辅助匹配）
                            dynamicFacetCatalogueName: facet.info.catalogueName,
                        });
                    }
                }
            });

            // 将文件与动态分面的映射关系添加到FormData（数组格式）
            if (fileFacetMappingList.length > 0) {
                uploadFormData.append(
                    'fileFacetMapping',
                    JSON.stringify(fileFacetMappingList)
                );
            }

            // 方式2：构建动态分面信息数组（用于向后兼容，或作为补充信息）
            // 获取所有唯一的动态分面信息（按catalogueName去重）[{"catalogueName":"案卷1"},{"catalogueName":"案卷1"}]
            const uniqueFacetInfos = Array.from(
                new Map(
                    dynamicFacets
                        .filter((f) => f.info.catalogueName)
                        .map((f) => [f.info.catalogueName, f.info])
                ).values()
            );

            // 将动态分面信息作为JSON字符串添加到FormData
            if (uniqueFacetInfos.length > 0) {
                uploadFormData.append(
                    'dynamicFacetInfoList',
                    JSON.stringify(uniqueFacetInfos)
                );
            }

            const uploadResponse = await axios.post(
                `/api/app/attachment/smart-classification/upload?catalogueId=${catalogueId}`,
                uploadFormData,
                {
                    headers: {
                        'Content-Type': 'multipart/form-data',
                    },
                }
            );

            setResults(uploadResponse.data);
            alert('上传成功！');
        } catch (error: any) {
            console.error('上传失败:', error);
            alert(
                `上传失败: ${
                    error.response?.data?.error?.message || error.message
                }`
            );
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{ padding: '20px' }}>
            <h2>智能分类文件上传</h2>

            <div style={{ marginBottom: '20px' }}>
                <label>
                    分类ID:
                    <input
                        type="text"
                        value={catalogueId}
                        onChange={(e) => setCatalogueId(e.target.value)}
                        placeholder="请输入分类ID"
                        style={{ marginLeft: '10px', padding: '5px' }}
                    />
                </label>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <label>
                    选择文件:
                    <input
                        type="file"
                        multiple
                        onChange={handleFileChange}
                        style={{ marginLeft: '10px' }}
                    />
                </label>
            </div>

            {/* 动态分面管理区域 */}
            <div style={{ marginBottom: '20px' }}>
                <div
                    style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        marginBottom: '10px',
                    }}
                >
                    <h3>动态分面管理（一个动态分面可以包含多个文件）</h3>
                    <button
                        onClick={createDynamicFacet}
                        style={{
                            padding: '5px 15px',
                            backgroundColor: '#28a745',
                            color: 'white',
                            border: 'none',
                            borderRadius: '4px',
                            cursor: 'pointer',
                        }}
                    >
                        + 创建动态分面
                    </button>
                </div>
                {dynamicFacets.map((facet) => (
                    <div
                        key={facet.key}
                        style={{
                            marginBottom: '15px',
                            padding: '15px',
                            border: '1px solid #007bff',
                            borderRadius: '4px',
                        }}
                    >
                        <div
                            style={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                alignItems: 'center',
                                marginBottom: '10px',
                            }}
                        >
                            <strong>
                                动态分面:{' '}
                                {facet.info.catalogueName || '(未命名)'}
                            </strong>
                            <button
                                onClick={() => deleteDynamicFacet(facet.key)}
                                style={{
                                    padding: '3px 10px',
                                    backgroundColor: '#dc3545',
                                    color: 'white',
                                    border: 'none',
                                    borderRadius: '4px',
                                    cursor: 'pointer',
                                    fontSize: '12px',
                                }}
                            >
                                删除
                            </button>
                        </div>
                        <div style={{ marginBottom: '10px' }}>
                            <label>
                                分类名称（如案卷名称）*:
                                <input
                                    type="text"
                                    value={facet.info.catalogueName}
                                    onChange={(e) =>
                                        updateDynamicFacet(facet.key, {
                                            catalogueName: e.target.value,
                                        })
                                    }
                                    placeholder="必填，如：案卷001"
                                    style={{
                                        marginLeft: '10px',
                                        padding: '5px',
                                        width: '300px',
                                    }}
                                />
                            </label>
                        </div>
                        <div style={{ marginBottom: '10px' }}>
                            <label>
                                描述:
                                <input
                                    type="text"
                                    value={facet.info.description || ''}
                                    onChange={(e) =>
                                        updateDynamicFacet(facet.key, {
                                            description: e.target.value,
                                        })
                                    }
                                    placeholder="可选"
                                    style={{
                                        marginLeft: '10px',
                                        padding: '5px',
                                        width: '300px',
                                    }}
                                />
                            </label>
                        </div>
                        <div>
                            <strong>
                                包含的文件 ({facet.fileIndices.length}个):
                            </strong>
                            {facet.fileIndices.length > 0 ? (
                                <ul
                                    style={{
                                        marginTop: '5px',
                                        paddingLeft: '20px',
                                    }}
                                >
                                    {facet.fileIndices.map((idx) => (
                                        <li key={idx}>{files[idx]?.name}</li>
                                    ))}
                                </ul>
                            ) : (
                                <span
                                    style={{
                                        color: '#999',
                                        marginLeft: '10px',
                                    }}
                                >
                                    暂无文件
                                </span>
                            )}
                        </div>
                    </div>
                ))}
            </div>

            {/* 文件列表 */}
            {files.length > 0 && (
                <div style={{ marginBottom: '20px' }}>
                    <h3>文件列表（{files.length}个文件）</h3>
                    {files.map((file, index) => (
                        <div
                            key={index}
                            style={{
                                marginBottom: '15px',
                                padding: '10px',
                                border: '1px solid #ccc',
                                borderRadius: '4px',
                            }}
                        >
                            <div
                                style={{
                                    display: 'flex',
                                    justifyContent: 'space-between',
                                    alignItems: 'center',
                                }}
                            >
                                <div>
                                    <strong>文件 {index + 1}:</strong>{' '}
                                    {file.name}
                                    {fileFacetMapping.get(index) && (
                                        <span
                                            style={{
                                                marginLeft: '10px',
                                                color: '#007bff',
                                                fontSize: '12px',
                                            }}
                                        >
                                            (已分配到:{' '}
                                            {dynamicFacets.find(
                                                (f) =>
                                                    f.key ===
                                                    fileFacetMapping.get(index)
                                            )?.info.catalogueName || '未知'}
                                            )
                                        </span>
                                    )}
                                </div>
                                <select
                                    value={fileFacetMapping.get(index) || ''}
                                    onChange={(e) =>
                                        assignFileToFacet(
                                            index,
                                            e.target.value || null
                                        )
                                    }
                                    style={{
                                        padding: '5px',
                                        minWidth: '200px',
                                    }}
                                >
                                    <option value="">
                                        -- 不分配动态分面 --
                                    </option>
                                    {dynamicFacets.map((facet) => (
                                        <option
                                            key={facet.key}
                                            value={facet.key}
                                        >
                                            {facet.info.catalogueName ||
                                                '(未命名)'}{' '}
                                            ({facet.fileIndices.length}个文件)
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            <button
                onClick={handleUpload}
                disabled={loading || files.length === 0 || !catalogueId}
                style={{
                    padding: '10px 20px',
                    backgroundColor: loading ? '#ccc' : '#007bff',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: loading ? 'not-allowed' : 'pointer',
                }}
            >
                {loading ? '上传中...' : '上传文件'}
            </button>

            {results.length > 0 && (
                <div style={{ marginTop: '20px' }}>
                    <h3>上传结果</h3>
                    <pre
                        style={{
                            background: '#f5f5f5',
                            padding: '10px',
                            overflow: 'auto',
                        }}
                    >
                        {JSON.stringify(results, null, 2)}
                    </pre>
                </div>
            )}
        </div>
    );
};

export default SmartClassificationUpload;
```

## 使用 Axios 的完整请求示例（支持一个动态分面包含多个文件）

由于需要同时发送文件和 JSON 数据，需要使用 multipart/form-data 格式：

```typescript
import axios from 'axios';

interface DynamicFacetInfo {
    catalogueName: string;
    description?: string;
    sequenceNumber?: number;
    tags?: string[];
    metadata?: Record<string, any>;
}

/**
 * 上传文件并进行智能分类
 * @param catalogueId 分类ID
 * @param files 文件数组
 * @param fileFacetMapping 文件与动态分面的映射关系（文件名 -> 动态分面分类名称），推荐使用此方式
 * @param dynamicFacetInfoList 动态分面信息数组（用于向后兼容，或作为补充信息）
 *                            多个文件可以共享同一个动态分面信息（相同的catalogueName）
 */
async function uploadFilesWithSmartClassification(
    catalogueId: string,
    files: File[],
    fileFacetMapping?: Record<string, string>,
    dynamicFacetInfoList?: DynamicFacetInfo[]
) {
    const formData = new FormData();

    // 添加文件
    files.forEach((file) => {
        formData.append('files', file);
    });

    // 方式1（推荐）：添加文件与动态分面的映射关系
    if (fileFacetMapping && Object.keys(fileFacetMapping).length > 0) {
        formData.append('fileFacetMapping', JSON.stringify(fileFacetMapping));
    }

    // 方式2：添加动态分面信息数组（用于向后兼容，或作为补充信息）
    if (dynamicFacetInfoList && dynamicFacetInfoList.length > 0) {
        formData.append(
            'dynamicFacetInfoList',
            JSON.stringify(dynamicFacetInfoList)
        );
    }

    try {
        const response = await axios.post(
            `/api/app/attachment/smart-classification/upload?catalogueId=${catalogueId}`,
            formData,
            {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
            }
        );

        return response.data;
    } catch (error) {
        console.error('上传失败:', error);
        throw error;
    }
}

// 使用示例：一个动态分面包含多个文件
const files = [
    new File(['content1'], 'file1.pdf'),
    new File(['content2'], 'file2.pdf'),
    new File(['content3'], 'file3.pdf'),
    new File(['content4'], 'file4.pdf'),
];

// 方式1（推荐）：使用fileFacetMapping明确文件与动态分面的映射关系
const fileFacetMapping: Record<string, string> = {
    'file1.pdf': '案卷001',
    'file2.pdf': '案卷001', // 与file1.pdf共享同一个动态分面
    'file3.pdf': '案卷002',
    'file4.pdf': '案卷002', // 与file3.pdf共享同一个动态分面
};

// 方式2：构建动态分面信息数组（用于向后兼容，或作为补充信息）
// 注意：这里只需要提供唯一的动态分面信息，不需要与文件一一对应
const dynamicFacetInfoList: DynamicFacetInfo[] = [
    {
        catalogueName: '案卷001',
        description: '第一个案卷',
        tags: ['重要', '2024'],
    },
    {
        catalogueName: '案卷002',
        description: '第二个案卷',
    },
];

uploadFilesWithSmartClassification(
    'your-catalogue-id',
    files,
    fileFacetMapping, // 推荐使用此方式
    dynamicFacetInfoList // 可选，用于向后兼容
)
    .then((results) => {
        console.log('上传成功:', results);
        // 结果中，文件1和文件2的AttachCatalogueId会指向同一个动态分面分类
        // 文件3和文件4的AttachCatalogueId会指向另一个动态分面分类
    })
    .catch((error) => {
        console.error('上传失败:', error);
    });
```

## 重要说明

1. **一个动态分面可以包含多个文件**：多个文件可以共享同一个动态分面信息（相同的`catalogueName`），后端会自动复用同一个动态分面分类，避免重复创建。

2. **文件与动态分面的映射方式**：

    - **推荐方式**：使用`fileFacetMapping`字段，通过文件索引和文件名明确映射到动态分面分类名称。这种方式支持文件夹结构，避免同名文件冲突。
        ```typescript
        // fileFacetMapping: [
        //   { fileName: "file1.pdf", fileIndex: 0, fileSize: 1024, dynamicFacetCatalogueName: "案卷001" },
        //   { fileName: "file1.pdf", fileIndex: 1, fileSize: 2048, dynamicFacetCatalogueName: "案卷002" }
        // ]
        // 注意：后端优先使用 fileIndex 匹配（最可靠），如果未提供则使用 fileName+fileSize 组合匹配
        ```
    - **补充方式**：使用`dynamicFacetInfoList`数组提供动态分面信息，用于创建动态分面分类。

3. **文件匹配机制**：后端优先使用文件索引匹配（最可靠），如果未提供索引则使用文件名+大小组合匹配。支持文件夹结构，避免同名文件冲突。

4. **可选参数**：如果文件不需要动态分面，可以不设置`DynamicFacetCatalogueName`或不在`fileFacetMapping`中指定。

5. **文件标记**：文件会通过`AttachCatalogueId`字段标记为属于对应的动态分面分类。属于同一个动态分面的多个文件会有相同的`AttachCatalogueId`。

6. **后端优化**：后端会在处理文件之前，先按`catalogueName`去重，创建/查找动态分面分类，然后缓存起来供后续文件复用，确保同一个批次中相同名称的动态分面只创建一次。

7. **请求格式**：使用`multipart/form-data`格式，文件通过 FormData 的`files`字段上传，文件与动态分面的映射通过`fileFacetMapping`字段以 JSON 对象形式上传，动态分面信息通过`dynamicFacetInfoList`字段以 JSON 数组形式上传。

## 后端接口修改建议

如果使用 multipart/form-data，需要修改 Controller 以支持从 FormData 中读取 JSON：

```csharp
[Route("smart-classification/upload")]
[HttpPost]
public virtual async Task<List<SmartClassificationResultDto>> CreateFilesWithSmartClassificationAsync(
    [FromQuery] Guid catalogueId,
    [FromQuery] string? prefix = null)
{
    var files = Request.Form.Files;
    var inputs = new List<AttachFileCreateDto>();

    foreach (var file in files)
    {
        using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        inputs.Add(new AttachFileCreateDto
        {
            FileAlias = file.FileName ?? Guid.NewGuid().ToString(),
            DocumentContent = memoryStream.ToArray(),
            SequenceNumber = null
        });
    }

    // 从FormData中读取dynamicFacetInfoList
    List<DynamicFacetInfoDto>? dynamicFacetInfoList = null;
    if (Request.Form.ContainsKey("dynamicFacetInfoList"))
    {
        var jsonString = Request.Form["dynamicFacetInfoList"].ToString();
        if (!string.IsNullOrEmpty(jsonString))
        {
            dynamicFacetInfoList = JsonSerializer.Deserialize<List<DynamicFacetInfoDto>>(jsonString);
        }
    }

    return await AttachCatalogueAppService.CreateFilesWithSmartClassificationAsync(
        catalogueId, inputs, prefix, dynamicFacetInfoList);
}
```
