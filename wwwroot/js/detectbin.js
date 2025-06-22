@{
  ViewData["Title"] = "Bin Plate Scanner";
}

<div class="container mt-5">
  <div class="row">
    <div class="col-md-8 offset-md-2">
      <div class="card">
        <div class="card-header bg-primary text-white">
          <h3 class="card-title">Bin Plate Scanner</h3>
        </div>
        <div class="card-body">
          <div class="text-center mb-4">
            <video id="scanner-video" width="100%" autoplay playsinline style="display:none;"></video>
            <canvas id="scanner-canvas" width="640" height="480" style="display:none;"></canvas>
            <img id="scanner-preview" src="#" alt="Preview" style="max-width:100%; display:none;" />
          </div>

          <div class="form-group">
            <label for="imageFile">Upload Bin Image</label>
            <input type="file" class="form-control-file" id="imageFile" accept="image/*">
          </div>

          <button id="scanButton" class="btn btn-primary btn-block">
            <i class="fas fa-camera"></i> Scan Bin Plate
          </button>

          <div id="results" class="mt-4" style="display:none;">
            <div class="alert alert-success">
              <h4>Scan Results</h4>
              <p><strong>Detected Plate ID:</strong> <span id="plateId"></span></p>
              <p><strong>Database Match:</strong> <span id="dbMatch"></span></p>
              <p><strong>Collection Record Created:</strong> <span id="recordId"></span></p>
            </div>
          </div>

          <div id="error" class="alert alert-danger mt-4" style="display:none;"></div>
        </div>
      </div>
    </div>
  </div>
</div>

@section Scripts {
  <script>
    $(document).ready(function() {
      $('#scanButton').click(function () {
        var fileInput = document.getElementById('imageFile');
        if (fileInput.files.length === 0) {
          showError('Please select an image file first.');
          return;
        }

        var file = fileInput.files[0];
        var formData = new FormData();
        formData.append('imageFile', file);

        // Show preview
        var reader = new FileReader();
        reader.onload = function (e) {
          $('#scanner-preview').attr('src', e.target.result).show();
        };
        reader.readAsDataURL(file);

        // Disable button during processing
        $('#scanButton').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Processing...');
        $('#error').hide();
        $('#results').hide();

        $.ajax({
          url: '/BinScan/Scan',
          type: 'POST',
          data: formData,
          processData: false,
          contentType: false,
          success: function (response) {
            if (response.success) {
              $('#plateId').text(response.detectedPlateId);
              $('#dbMatch').text('Found (Bin ID: ' + response.binId + ')');
              $('#recordId').text(response.collectionRecordId);
              $('#results').show();
            } else {
              showError(response.message);
              if (response.detectedPlateId) {
                $('#plateId').text(response.detectedPlateId);
                $('#dbMatch').text('Not found in database');
                $('#results').show();
              }
            }
          },
          error: function (xhr) {
            showError(xhr.responseText || 'An error occurred during scanning.');
          },
          complete: function () {
            $('#scanButton').prop('disabled', false).html('<i class="fas fa-camera"></i> Scan Bin Plate');
          }
        });
      });

    function showError(message) {
      $('#error').text(message).show();
            }
        });
  </script>
}
