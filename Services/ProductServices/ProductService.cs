﻿using AutoMapper;
using Blink_API.DTOs.ProductDTOs;
using Blink_API.DTOs.TransferProductsDTOs;
using Blink_API.Errors;
using Blink_API.Models;
using Blink_API.Services.ProductServices;
namespace Blink_API.Services.Product
{
    public class ProductService
    {
        private readonly UnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment _IWebHostEnvironment;
        private readonly ProductTransferService productTransferService;
        public ProductService(UnitOfWork _unitOfWork,IMapper _mapper, IWebHostEnvironment iWebHostEnvironment, ProductTransferService _productTransferService)
        {
            unitOfWork = _unitOfWork;
            mapper = _mapper;
            _IWebHostEnvironment = iWebHostEnvironment;
            productTransferService = _productTransferService;
        }
        public async Task<ICollection<ProductDiscountsDTO>> GetAll()
        {
            var products = await unitOfWork.ProductRepo.GetAll();
            var result = mapper.Map<ICollection<ProductDiscountsDTO>>(products);
            return result;
        }
        public async Task<int> GetPagesCount(int pgSize)
        {
            var count = await unitOfWork.ProductRepo.GetPagesCount(pgSize);
            return count;
        }
        public async Task<int> GetPagesCountWithUser(int pgSize,string UserId)
        {
            var count = await unitOfWork.ProductRepo.GetPagesCountWithUser(pgSize,UserId);
            return count;
        }
        public async Task<ICollection<ProductDiscountsDTO>> GetAllPagginated(int pgNumber,int pgSize)
        {
            var products = await unitOfWork.ProductRepo.GetAllPagginated(pgNumber, pgSize);
            var result = mapper.Map<ICollection<ProductDiscountsDTO>>(products);
            return result;
        }
        public async Task<ICollection<ProductDiscountsDTO>> GetAllPagginatedWithUser(int pgNumber, int pgSize,string UserId)
        {
            var products = await unitOfWork.ProductRepo.GetAllPagginatedWithUser(pgNumber, pgSize,UserId);
            var result = mapper.Map<ICollection<ProductDiscountsDTO>>(products);
            return result;
        }
        public async Task<ICollection<ProductDiscountsDTO>> GetFilteredProducts(string filter, int pgNumber, int pgSize)
        {
            var products = await unitOfWork.ProductRepo.GetFilteredProducts(filter, pgNumber, pgSize);
            var result = mapper.Map<ICollection<ProductDiscountsDTO>>(products);
            return result;
        }
        public async Task<ProductDiscountsDTO> GetById(int id)
        {
            var product = await unitOfWork.ProductRepo.GetById(id);
            var result = mapper.Map<ProductDiscountsDTO>(product);
            return result;
        }
        public async Task<ICollection<ProductDiscountsDTO>> GetByChildCategoryId(int id)
        {
            var products = await unitOfWork.ProductRepo.GetByChildCategoryId(id);
            var result = mapper.Map<ICollection<ProductDiscountsDTO>>(products);
            return result;
        }
        public async Task<ICollection<ProductDiscountsDTO>> GetByParentCategoryId(int id)
        {
            var products = await unitOfWork.ProductRepo.GetByParentCategoryId(id);
            var result = mapper.Map<ICollection<ProductDiscountsDTO>>(products);
            return result;
        }
        public async Task<int> Add(InsertProductDTO productDTO)
        {
            if (productDTO == null)
                return 0;
            var mappedProduct = mapper.Map<Models.Product>(productDTO);
            ICollection<ProductImage> ProductImagesList = await CheckImagesToSaveInInsert(mappedProduct, productDTO.ProductImages);
            mappedProduct.ProductImages = ProductImagesList;
            var mappedStockProducts = mapper.Map<ICollection<StockProductInventory>>(productDTO.ProductStocks);
            mappedProduct.StockProductInventories = mappedStockProducts;
            var productId = await unitOfWork.ProductRepo.AddProduct(mappedProduct);
            if(productId > 0)
            {
                #region Add Product To Inventory Transaction Header
                List<InsertInputTrasactionProductDTO> newListProducts = new List<InsertInputTrasactionProductDTO>();
                newListProducts.Add(new InsertInputTrasactionProductDTO { TransactionQuantity = mappedStockProducts.Sum(s => s.StockQuantity), ProductId = productId });
                InsertInputTransferProductDTO newTransaction = new InsertInputTransferProductDTO()
                {
                    InventoryTransactionDate = DateTime.UtcNow,
                    InventoryTransactionType = 1,
                    TransactionProducts = newListProducts
                };
                await productTransferService.AddInputInventory(newTransaction);
                #endregion
            }
            return productId;
        }
        public async Task<bool> Update(int id, UpdateProductDTO productDTO)
        {
            if (productDTO == null)
                return false;
            var product = mapper.Map<Models.Product>(productDTO);
            int ProductId = id;
            List<InsertProductImagesDTO> ProductImageList = await CheckImagesToSaveInUpdate(id,productDTO.NewProductImages,productDTO.OldProductImages);
            bool result = await unitOfWork.ProductRepo.UpdateProduct(id, product);
            if (result)
            {
                var mappedStockProducts = mapper.Map<ICollection<StockProductInventory>>(productDTO.ProductStocks);
                foreach (var stockProduct in mappedStockProducts)
                {
                    stockProduct.ProductId = ProductId;
                }
                await unitOfWork.ProductRepo.UpdateStockProducts(mappedStockProducts);
                await AddProductImage(ProductImageList);
            }
            return result;
        }
        public async Task Delete(int id)
        {
            if (id <= 0)
                return;
            await unitOfWork.ProductRepo.Delete(id);
        }
        public async Task AddProductImage(List<InsertProductImagesDTO> productImagesDTO)
        {
            var prdImages = mapper.Map<List<ProductImage>>(productImagesDTO);
            await unitOfWork.ProductRepo.AddProductImage(prdImages);
        }
        private async Task<string> SaveFileAsync(IFormFile file)
        {
            // 1. Correct path construction
            var uploadFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "products"
            );

            // 2. Create directory if needed
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            // 3. Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            // 4. Save file
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            // 5. Return web-accessible path
            return $"/images/products/{uniqueFileName}";
        }
        private async Task<List<ProductImage>> CheckImagesToSaveInInsert(Models.Product product, List<IFormFile> images)
        {
            var result = new List<ProductImage>();
            foreach(var img in images)
            {
                if(img is IFormFile file)
                {
                    string path = await SaveFileAsync(file);
                    result.Add(new ProductImage { ProductImagePath = path,Product= product });
                }
            }
            return result;
        }
        private async Task<List<InsertProductImagesDTO>> CheckImagesToSaveInUpdate(int id, List<IFormFile> newImages, List<string> oldImages)
        {
            var allImages = new List<IFormFile>();
            var deleteImagePaths = new List<string>();
            var result = new List<InsertProductImagesDTO>();
            foreach (var imgUrl in oldImages)
            {
                var physicalPath = GetPhysicalPathFromUrl(imgUrl);
                if (!string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath))
                {
                    var file = GetFormFileFromDisk(physicalPath);
                    if (file != null)
                        allImages.Add(file);
                }
            }
            var oldProductImages = await unitOfWork.ProductRepo.GetProductImages(id);
            foreach (var productImage in oldProductImages)
            {
                var physicalPath = GetPhysicalPathFromUrl(productImage.ProductImagePath);
                if (!string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath))
                {
                    deleteImagePaths.Add(physicalPath);
                }
            }
            allImages.AddRange(newImages);
            foreach (var path in deleteImagePaths)
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                }
            }
            await unitOfWork.ProductRepo.DeleteOldProductImages(id);
            foreach (var file in allImages)
            {
                var path = await SaveFileAsync(file);
                result.Add(new InsertProductImagesDTO
                {
                    ProductId = id,
                    ProductImagePath = path
                });
            }
            return result;
        }
        private string GetPhysicalPathFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            int startIndex = url.IndexOf("/images/");
            if (startIndex == -1) return null;
            string relativePath = url.Substring(startIndex + 1);
            return Path.Combine(_IWebHostEnvironment.WebRootPath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
        public IFormFile GetFormFileFromDisk(string path)
        {
            var fileName = Path.GetFileName(path);
            var extension = Path.GetExtension(path).ToLower();
            string contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
            byte[] fileBytes = File.ReadAllBytes(path);
            var stream = new MemoryStream(fileBytes); 

            return new FormFile(stream, 0, fileBytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

        }
        public async Task<ICollection<ReadFilterAttributesDTO>> GetFilterAttributesAsync()
        {
            var result = await unitOfWork.ProductRepo.GetFilterAttributeAsync();
            var mapped = mapper.Map<ICollection<ReadFilterAttributesDTO>>(result);
            return mapped;
        }
        public async Task<FilterAttributes?> GetFilterAttributeById(int id)
        {
            var result = await unitOfWork.ProductRepo.GetFilterAttributeById(id);
            return result;
        }
        public async Task<ApiResponse> AddFilterAttribute(InsertFilterAttributeDTO filterAttribute)
        {
            var mappedAttribute=mapper.Map<FilterAttributes>(filterAttribute);  
            int AttributeId = await unitOfWork.ProductRepo.AddFilterAttribute(mappedAttribute);
            if (AttributeId == 0)
                throw new Exception("There is an error occured");
            return new ApiResponse(200, "FilterAttribute Saved Success");
        }
        public async Task<ICollection<DefaultAttributes>> GetDefaultAttributesByAttributeId(int id)
        {
            var result = await unitOfWork.ProductRepo.GetDefaultAttributesByAttributeId(id);
            return result;
        }
        public async Task AddDefaultAttribute(InsertDefaultAttributesDTO defaultAttributes)
        {
            var mappedDefaultAttributes=mapper.Map<DefaultAttributes>(defaultAttributes);
            await unitOfWork.ProductRepo.AddDefaultAttribute(mappedDefaultAttributes);
        }
        public async Task AddProductAttribute(ICollection<InsertProductAttributeDTO> insertProductAttributeDTO)
        {
            var mappedProductAttribute = mapper.Map<ICollection<ProductAttributes>>(insertProductAttributeDTO);
            await unitOfWork.ProductRepo.AddProductAttribute(mappedProductAttribute);
        }
        public async Task<ICollection<InsertProductAttributeDTO>> GetProductAttributes(int productId)
        {
            var productAttributes = await unitOfWork.ProductRepo.GetProductAttributes(productId);
            var mappedProductAttributes = mapper.Map<ICollection<InsertProductAttributeDTO>>(productAttributes);
            return mappedProductAttributes;
        }
        public async Task DeleteProductAttributes(int productId)
        {
            await unitOfWork.ProductRepo.DeleteOldProductAttributes(productId);
        }
        public async Task<ICollection<ProductDiscountsDTO>> GetFillteredProducts(Dictionary<int, List<string>> filtersProduct, int pgNumber, decimal fromPrice, decimal toPrice, int rating, int categoryId)
        {
            var products = await unitOfWork.ProductRepo.GetFillteredProducts(filtersProduct,pgNumber,fromPrice,toPrice,rating,categoryId);
            var mappedProducts = mapper.Map<ICollection<ProductDiscountsDTO>>(products);
            return mappedProducts;
        }
        public async Task<ICollection<StockProductInventory>> GetProductStock(int ProductId)
        {
            var productStock = await unitOfWork.ProductRepo.GetProductStock(ProductId);
            return productStock;
        }
        #region Sprint3
        public async Task<bool> CheckUserAvailableToReview(string userId, int productId)
        {
            if (userId == null & productId <= 0)
                return false;
            return await unitOfWork.ProductRepo.CheckUserAvailableToReview(userId,productId);
        }
        public async Task<int?> GetProductStockInInventory(int SourceId,int productId)
        {
            //var product = await unitOfWork.ProductRepo.GetProductStockInInventory(SourceId, productId);
            //var mappedProduct= mapper.Map<ReadProductInStockDTO>(product);
            //return mappedProduct;

            var result = await unitOfWork.ProductRepo.GetProductStockInInventory(SourceId, productId);
            return result;

        }
        public async Task<List<Brand>> GetListOfBrands()
        {
            return await unitOfWork.ProductRepo.GetListOfBrands();
        }
        public async Task<List<Category>> GetSubCategories()
        {
            return await unitOfWork.ProductRepo.GetSubCategories();
        }
        public async Task<List<Inventory>> GetListOfInventory()
        {
            return await unitOfWork.ProductRepo.GetListOfInventory();
        }
        public async Task<bool> DeleteProductImage(int ProductId,string imagePath)
        {
            string decodedUrl = Uri.UnescapeDataString(imagePath);
            int startIndex = decodedUrl.IndexOf("/images/", StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
                return false;
            string relativePath = decodedUrl.Substring(startIndex).TrimStart('/');
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            bool isDeleted = false;
            if (File.Exists(fullPath))
            {
                relativePath = "/" + relativePath;
                isDeleted = await unitOfWork.ProductRepo.DeleteProductImage(ProductId, relativePath.Replace("\\", "/"));
                if (isDeleted)
                {
                    File.Delete(fullPath);
                }
            }
            return isDeleted;
        }
        public async Task<List<ReadReviewSuppliedProductDTO>> GetSuppliedProductsByUserID(int pgNumber, string UserId)
        {
            var result = await unitOfWork.ProductRepo.GetSuppliedProductsByUserID(pgNumber, UserId);
            var mappedReviewSuppliedProducts = mapper.Map<List<ReadReviewSuppliedProductDTO>>(result);
            return mappedReviewSuppliedProducts;
        }
        public int GetTotalPageForReviewProducts(string UserId)
        {
            return unitOfWork.ProductRepo.GetTotalPageForReviewProducts(UserId);
        }
        public async Task<List<ProductDiscountsDTO>> SearchProducts(string searchText)
        {
            var result = await unitOfWork.ProductRepo.SearchProducts(searchText);
            var mappedResult = mapper.Map<List<ProductDiscountsDTO>>(result);
            return mappedResult;    
        }
        public async Task<List<ProductDiscountsDTO>> SearchProducts(string searchText,int inventoryId)
        {
            var result = await unitOfWork.ProductRepo.SearchProducts(searchText, inventoryId);
            var mappedResult = mapper.Map<List<ProductDiscountsDTO>>(result);
            return mappedResult;
        }
        public async Task<List<ProductDiscountsDTO>> GetProductsByBrandId(int id)
        {
            var result = await unitOfWork.ProductRepo.GetProductsByBrandId(id);
            var mappedResult = mapper.Map<List<ProductDiscountsDTO>>(result);
            return mappedResult;
        }
        public async Task<List<ProductDiscountsDTO>> GetProductsByCategoryId(int id)
        {
            var result = await unitOfWork.ProductRepo.GetProductsByCategoryId(id);
            var mappedResult = mapper.Map<List<ProductDiscountsDTO>>(result);
            return mappedResult;
        }
        public async Task <List<ReadProductsDataDTO>> GetProductsByInventoryId(int id)
        {
            var result = await unitOfWork.ProductRepo.GetProductsByInventoryId(id);
            var mappedResult = mapper.Map<List<ReadProductsDataDTO>>(result);
            return mappedResult;
        }
        #endregion
    }
}
